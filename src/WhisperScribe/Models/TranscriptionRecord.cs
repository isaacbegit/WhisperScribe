namespace WhisperScribe.Models;

/// <summary>
/// A completed transcription as persisted in / read back from SQLite (Tab 2 "Converted" source).
/// </summary>
public class TranscriptionRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public double DurationSeconds { get; set; }
    public string Language { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public string HardwareUsed { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public double ProcessingSeconds { get; set; }
    public string PlainText { get; set; } = string.Empty;
    public string SrtText { get; set; } = string.Empty;
    public string JsonText { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
}
