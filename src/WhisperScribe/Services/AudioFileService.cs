using System.IO;
using NAudio.Wave;

namespace WhisperScribe.Services;

public record AudioProbeResult(TimeSpan? Duration, long SizeBytes);

/// <summary>
/// Handles filesystem probing (size/duration) and format normalization: Whisper.net requires
/// 16kHz mono PCM WAV input, so any supported source format is transcoded via NAudio first.
/// </summary>
public class AudioFileService
{
    public static readonly string[] SupportedExtensions =
        { ".mp3", ".wav", ".m4a", ".aac", ".flac", ".ogg", ".wma", ".opus", ".mp4", ".mkv" };

    public AudioProbeResult Probe(string filePath)
    {
        long size = new FileInfo(filePath).Length;
        TimeSpan? duration = null;

        try
        {
            using var reader = new AudioFileReader(filePath);
            duration = reader.TotalTime;
        }
        catch
        {
            // Some containers (e.g. certain .mp4/.mkv audio tracks) need MediaFoundationReader instead.
            try
            {
                using var mfReader = new MediaFoundationReader(filePath);
                duration = mfReader.TotalTime;
            }
            catch
            {
                // Leave duration null — it will simply show as "—" in the grid; transcription can still proceed.
            }
        }

        return new AudioProbeResult(duration, size);
    }

    /// <summary>
    /// Converts any supported input file to a temporary 16kHz mono PCM16 WAV file, as required by Whisper.
    /// Caller is responsible for deleting the returned temp file once done.
    /// </summary>
    public string ConvertToWhisperWav(string sourcePath)
    {
        var targetFormat = new WaveFormat(16000, 16, 1);
        var tempPath = Path.Combine(Path.GetTempPath(), $"whisperscribe_{Guid.NewGuid():N}.wav");

        using WaveStream reader = CreateReader(sourcePath);
        var resampler = new NAudio.Wave.SampleProviders.WdlResamplingSampleProvider(
            reader.ToSampleProvider(), targetFormat.SampleRate);

        var mono = reader.WaveFormat.Channels == 1
            ? (ISampleProvider)resampler
            : new NAudio.Wave.SampleProviders.StereoToMonoSampleProvider(resampler);

        WaveFileWriter.CreateWaveFile16(tempPath, mono);
        return tempPath;
    }

    private static WaveStream CreateReader(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".wav" => new WaveFileReader(path),
            ".mp3" => new Mp3FileReader(path),
            _ => new MediaFoundationReader(path) // handles m4a, aac, wma, mp4, mkv, and more via Windows Media Foundation
        };
    }
}
