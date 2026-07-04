using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WhisperScribe.Models;
using WhisperScribe.Services;

namespace WhisperScribe.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly WhisperModelManager _modelManager = App.Models;
    private readonly DatabaseService _db = App.Database;

    public ObservableCollection<WhisperModelInfo> Models { get; } = new();

    [ObservableProperty] private WhisperModelInfo? _selectedModel;
    [ObservableProperty] private bool _isAnyDownloadInProgress;
    [ObservableProperty] private string _statusBarMessage = "Idle";
    [ObservableProperty] private double _statusBarProgress;
    [ObservableProperty] private string _modelsRootPath = App.Models.ModelsFolder;

    /// <summary>Raised whenever a model finishes downloading/deleting, so the Transcript tab can refresh readiness.</summary>
    public event EventHandler? CatalogChanged;

    public SettingsViewModel()
    {
        Reload();
    }

    public void Reload()
    {
        Models.Clear();
        foreach (var m in _modelManager.GetCatalog())
            Models.Add(m);
        SelectedModel ??= Models.FirstOrDefault();
        ModelsRootPath = _modelManager.ModelsFolder;
    }

    [RelayCommand]
    private void BrowseModelsFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose a folder to store downloaded Whisper models",
            Multiselect = false,
            InitialDirectory = ModelsRootPath
        };

        if (dialog.ShowDialog() != true) return;

        _modelManager.SetModelsFolder(dialog.FolderName);
        _db.SetSetting("ModelsFolder", dialog.FolderName);
        StatusBarMessage = $"Models folder set to {dialog.FolderName}";
        Reload();
        CatalogChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task DownloadModelAsync(WhisperModelInfo? model)
    {
        model ??= SelectedModel;
        if (model is null || model.IsDownloaded || model.IsDownloading) return;

        model.IsDownloading = true;
        model.DownloadProgress = 0;
        IsAnyDownloadInProgress = true;
        StatusBarMessage = $"Downloading {model.DisplayName}…";

        var progress = new Progress<double>(p =>
        {
            model.DownloadProgress = p;
            StatusBarProgress = p;
        });

        try
        {
            await _modelManager.DownloadModelAsync(model, progress, CancellationToken.None);
            StatusBarMessage = $"{model.DisplayName} ready.";
            CatalogChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusBarMessage = $"Download failed: {ex.Message}";
        }
        finally
        {
            model.IsDownloading = false;
            IsAnyDownloadInProgress = Models.Any(m => m.IsDownloading);
            StatusBarProgress = 0;
        }
    }

    [RelayCommand]
    private void DeleteModel(WhisperModelInfo? model)
    {
        model ??= SelectedModel;
        if (model is null || !model.IsDownloaded) return;
        _modelManager.DeleteModel(model);
        StatusBarMessage = $"Removed {model.DisplayName}.";
        CatalogChanged?.Invoke(this, EventArgs.Empty);
    }
}
