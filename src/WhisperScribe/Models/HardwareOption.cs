namespace WhisperScribe.Models;

public enum HardwareKind
{
    Cpu,
    Gpu
}

/// <summary>
/// One selectable compute device for running the Whisper model (populated by HardwareDetectionService).
/// </summary>
public class HardwareOption
{
    public HardwareKind Kind { get; init; }
    public string DisplayName { get; init; } = string.Empty;   // e.g. "NVIDIA GeForce RTX 4070 (GPU)"  or "CPU (12 threads)"
    public string DeviceId { get; init; } = string.Empty;      // internal identifier used when configuring Whisper.net
    public bool IsRecommended { get; init; }

    public override string ToString() => DisplayName;
}
