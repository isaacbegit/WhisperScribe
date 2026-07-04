using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WhisperScribe.Models;
using WhisperScribe.Services;

namespace WhisperScribe.ViewModels;

public partial class TranscriptViewModel : ObservableObject
{
    private readonly DatabaseService _db = App.Database;
    private readonly HardwareDetectionService _hardwareService = App.Hardware;
    private readonly WhisperModelManager _modelManager = App.Models;
    private readonly TranscriptionService _transcription = App.Transcription;
    private readonly AudioFileService _audioFiles = App.AudioFiles;
    private readonly ExportService _export = App.Export;

    public ObservableCollection<AudioFileItem> Files { get; } = new();
    public ObservableCollection<HardwareOption> HardwareOptions { get; } = new();
    public ObservableCollection<string> Languages { get; } = new();
    public ObservableCollection<WhisperModelInfo> AvailableModels { get; } = new();

    // NotifyCanExecuteChangedFor makes the generated commands re-evaluate CanExecute
    // automatically whenever these properties change (RelayCommand does NOT auto-requery
    // like WPF's built-in RoutedCommand does — this was the cause of the buttons staying
    // disabled after picking a file).
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertSelectedCommand))]
    private AudioFileItem? _selectedFile;

    [ObservableProperty] private HardwareOption? _selectedHardware;
    [ObservableProperty] private string _selectedLanguage = "Auto-detect";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConvertAllCommand))]
    private WhisperModelInfo? _selectedModel;

    [ObservableProperty] private double _overallProgress;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConvertAllCommand))]
    private bool _isConverting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConvertAllCommand))]
    private bool _isModelReady;

    [ObservableProperty] private string _modelStatusMessage = "No model downloaded yet — visit Settings.";

    public TranscriptViewModel()
    {
        foreach (var hw in _hardwareService.GetAvailableHardware())
            HardwareOptions.Add(hw);
        SelectedHardware = _hardwareService.GetRecommendedDefault(HardwareOptions);

        Languages.Add("Auto-detect");
        foreach (var lang in WhisperLanguages.All)
            Languages.Add(lang);
        SelectedLanguage = "Auto-detect";

        RefreshModels();
    }

    public void RefreshModels()
    {
        AvailableModels.Clear();
        foreach (var m in _modelManager.GetCatalog())
            AvailableModels.Add(m);

        // Prefer keeping the current selection if it still exists in the refreshed catalog,
        // otherwise fall back to the first downloaded model, otherwise just the first one.
        var previous = SelectedModel?.Name;
        SelectedModel = AvailableModels.FirstOrDefault(m => m.Name == previous)
                         ?? AvailableModels.FirstOrDefault(m => m.IsDownloaded)
                         ?? AvailableModels.FirstOrDefault();
        UpdateModelReadiness();
    }

    partial void OnSelectedModelChanged(WhisperModelInfo? value) => UpdateModelReadiness();

    private void UpdateModelReadiness()
    {
        IsModelReady = SelectedModel?.IsDownloaded == true;
        ModelStatusMessage = SelectedModel is null
            ? "No model selected — visit Settings to download one."
            : IsModelReady
                ? $"Model ready: {SelectedModel.DisplayName}"
                : $"\"{SelectedModel.DisplayName}\" isn't downloaded yet. Go to Settings and download it before converting.";
    }

    [RelayCommand]
    private void BrowseFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select audio files",
            Multiselect = true,
            Filter = "Supported audio files|" + string.Join(";", AudioFileService.SupportedExtensions.Select(e => "*" + e)) +
                     "|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true) return;

        foreach (var path in dialog.FileNames)
        {
            if (Files.Any(f => f.FilePath == path)) continue;

            var probe = _audioFiles.Probe(path);
            Files.Add(new AudioFileItem
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                FileSizeBytes = probe.SizeBytes,
                Duration = probe.Duration,
                DetectedLanguage = "—",
                Status = TranscriptionStatus.Pending,
                StatusMessage = "Ready"
            });
        }

        ConvertAllCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveFile(AudioFileItem? item)
    {
        if (item is null) return;
        Files.Remove(item);
        ConvertAllCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
        ConvertAllCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanConvertSelected))]
    private async Task ConvertSelectedAsync()
    {
        if (!EnsureModelReady()) return;
        if (SelectedFile is not null)
            await ConvertFilesAsync(new[] { SelectedFile });
    }

    [RelayCommand(CanExecute = nameof(CanConvertAll))]
    private async Task ConvertAllAsync()
    {
        if (!EnsureModelReady()) return;
        await ConvertFilesAsync(Files.Where(f => f.Status != TranscriptionStatus.Completed).ToList());
    }

    private bool CanConvertSelected() => IsModelReady && !IsConverting && SelectedFile is not null;
    private bool CanConvertAll() => IsModelReady && !IsConverting && Files.Count > 0;

    /// <summary>Defensive re-check in case a command somehow fires while the model isn't ready.</summary>
    private bool EnsureModelReady()
    {
        if (IsModelReady) return true;
        ModelStatusMessage = SelectedModel is null
            ? "No model selected — visit Settings to download one before converting."
            : $"\"{SelectedModel.DisplayName}\" isn't downloaded yet. Go to Settings and download it before converting.";
        return false;
    }

    private async Task ConvertFilesAsync(IReadOnlyList<AudioFileItem> items)
    {
        if (items.Count == 0 || SelectedModel is null || SelectedHardware is null) return;

        IsConverting = true;
        try
        {
            int completed = 0;
            foreach (var item in items)
            {
                item.Status = TranscriptionStatus.Processing;
                item.StatusMessage = "Converting audio…";
                item.ProgressPercent = 5;

                string? tempWav = null;
                try
                {
                    tempWav = await Task.Run(() => _audioFiles.ConvertToWhisperWav(item.FilePath));

                    item.StatusMessage = "Transcribing…";
                    var langCode = SelectedLanguage == "Auto-detect" ? "auto" : WhisperLanguages.ToCode(SelectedLanguage);

                    var progress = new Progress<double>(p => item.ProgressPercent = Math.Max(10, p));
                    var outcome = await _transcription.TranscribeAsync(
                        tempWav, SelectedModel, SelectedHardware, langCode, progress, CancellationToken.None);

                    item.DetectedLanguage = WhisperLanguages.FromCode(outcome.DetectedLanguage);
                    item.PlainText = outcome.PlainText;
                    item.SrtText = outcome.SrtText;
                    item.JsonText = outcome.JsonText;
                    item.Status = TranscriptionStatus.Completed;
                    item.StatusMessage = "Completed";
                    item.ProgressPercent = 100;

                    var record = new Models.TranscriptionRecord
                    {
                        FileName = item.FileName,
                        SourcePath = item.FilePath,
                        FileSizeBytes = item.FileSizeBytes,
                        DurationSeconds = item.Duration?.TotalSeconds ?? 0,
                        Language = item.DetectedLanguage,
                        ModelUsed = SelectedModel.DisplayName,
                        HardwareUsed = SelectedHardware.DisplayName,
                        CreatedAtUtc = DateTime.UtcNow,
                        PlainText = item.PlainText ?? "",
                        SrtText = item.SrtText ?? "",
                        JsonText = item.JsonText ?? "",
                        Status = "Completed"
                    };
                    item.DatabaseId = _db.InsertTranscription(record);
                }
                catch (Exception ex)
                {
                    item.Status = TranscriptionStatus.Failed;
                    item.StatusMessage = $"Failed: {ex.Message}";
                }
                finally
                {
                    if (tempWav is not null && File.Exists(tempWav))
                        File.Delete(tempWav);
                }

                completed++;
                OverallProgress = (double)completed / items.Count * 100.0;
            }
        }
        finally
        {
            IsConverting = false;
            OverallProgress = 0;
        }
    }

    [RelayCommand]
    private void ExportPlainText(AudioFileItem? item)
    {
        item ??= SelectedFile;
        if (item?.PlainText is not null) _export.ExportPlainText(item.PlainText, Path.GetFileNameWithoutExtension(item.FileName));
    }

    [RelayCommand]
    private void ExportSrt(AudioFileItem? item)
    {
        item ??= SelectedFile;
        if (item?.SrtText is not null) _export.ExportSrt(item.SrtText, Path.GetFileNameWithoutExtension(item.FileName));
    }

    [RelayCommand]
    private void ExportJson(AudioFileItem? item)
    {
        item ??= SelectedFile;
        if (item?.JsonText is not null) _export.ExportJson(item.JsonText, Path.GetFileNameWithoutExtension(item.FileName));
    }
}

/// <summary>Minimal English display-name &lt;-&gt; ISO-639-1 code map for Whisper's supported languages.</summary>
public static class WhisperLanguages
{
    private static readonly (string Name, string Code)[] Map =
    {
        ("English","en"), ("Arabic","ar"), ("Spanish","es"), ("French","fr"), ("German","de"),
        ("Italian","it"), ("Portuguese","pt"), ("Russian","ru"), ("Chinese","zh"), ("Japanese","ja"),
        ("Korean","ko"), ("Turkish","tr"), ("Dutch","nl"), ("Polish","pl"), ("Swedish","sv"),
        ("Hindi","hi"), ("Ukrainian","uk"), ("Greek","el"), ("Hebrew","he"), ("Indonesian","id"),
        ("Vietnamese","vi"), ("Thai","th"), ("Czech","cs"), ("Romanian","ro"), ("Danish","da"),
        ("Finnish","fi"), ("Norwegian","no"), ("Hungarian","hu")
    };

    public static IEnumerable<string> All => Map.Select(m => m.Name);
    public static string ToCode(string name) => Map.FirstOrDefault(m => m.Name == name).Code ?? "en";
    public static string FromCode(string code) => Map.FirstOrDefault(m => m.Code == code).Name is { } n && n != null ? n : code;
}
