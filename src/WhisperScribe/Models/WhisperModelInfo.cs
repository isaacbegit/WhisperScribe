using CommunityToolkit.Mvvm.ComponentModel;

namespace WhisperScribe.Models;

/// <summary>
/// Describes one of the official ggml Whisper models available for local download.
/// Files are pulled from the ggerganov/whisper.cpp Hugging Face repository.
/// </summary>
public partial class WhisperModelInfo : ObservableObject
{
    public string Name { get; init; } = string.Empty;           // e.g. "base.en"
    public string DisplayName { get; init; } = string.Empty;    // e.g. "Base (English)"
    public string FileName { get; init; } = string.Empty;       // e.g. "ggml-base.en.bin"
    public string DownloadUrl { get; init; } = string.Empty;
    public long ApproxSizeBytes { get; init; }
    public string SizeDisplay => $"{ApproxSizeBytes / 1024.0 / 1024.0:0} MB";
    public string Description { get; init; } = string.Empty;

    [ObservableProperty] private bool _isDownloaded;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private string _localPath = string.Empty;
}
