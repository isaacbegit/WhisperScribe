using CommunityToolkit.Mvvm.ComponentModel;

namespace WhisperScribe.Models;

public enum TranscriptionStatus
{
    Pending,
    Queued,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Represents a single audio file staged (or already processed) in the Transcript tab's queue.
/// </summary>
public partial class AudioFileItem : ObservableObject
{
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private long _fileSizeBytes;
    [ObservableProperty] private TimeSpan? _duration;
    [ObservableProperty] private string _detectedLanguage = "—";
    [ObservableProperty] private TranscriptionStatus _status = TranscriptionStatus.Pending;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusMessage = "Ready";

    // Populated once transcription finishes; also mirrored to the database.
    [ObservableProperty] private string? _plainText;
    [ObservableProperty] private string? _srtText;
    [ObservableProperty] private string? _jsonText;
    [ObservableProperty] private int? _databaseId;

    public string FileSizeDisplay => FormatSize(FileSizeBytes);
    public string DurationDisplay => Duration.HasValue ? Duration.Value.ToString(@"hh\:mm\:ss") : "—";

    partial void OnFileSizeBytesChanged(long value) => OnPropertyChanged(nameof(FileSizeDisplay));
    partial void OnDurationChanged(TimeSpan? value) => OnPropertyChanged(nameof(DurationDisplay));

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.#} {units[unit]}";
    }
}
