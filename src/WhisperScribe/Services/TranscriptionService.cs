using System.IO;
using System.Text;
using System.Text.Json;
using Whisper.net;
using Whisper.net.Ggml;
using WhisperScribe.Models;

namespace WhisperScribe.Services;

public record TranscriptionSegment(TimeSpan Start, TimeSpan End, string Text);

public record TranscriptionOutcome(
    string DetectedLanguage,
    string PlainText,
    string SrtText,
    string JsonText,
    List<TranscriptionSegment> Segments);

/// <summary>
/// Wraps Whisper.net to run local speech-to-text inference against a downloaded ggml model.
/// Whisper.net automatically uses the CUDA runtime (Whisper.net.Runtime.Cuda) when available and
/// requested; otherwise it falls back to the CPU runtime.
/// </summary>
public class TranscriptionService
{
    private readonly WhisperModelManager _modelManager;

    public TranscriptionService(WhisperModelManager modelManager)
    {
        _modelManager = modelManager;
    }

    /// <summary>
    /// Transcribes a single (already-normalized to 16kHz mono WAV) audio stream.
    /// language: null/"auto" triggers Whisper's language auto-detection.
    /// </summary>
    public async Task<TranscriptionOutcome> TranscribeAsync(
        string wavFilePath,
        WhisperModelInfo model,
        HardwareOption hardware,
        string? language,
        IProgress<double> progress,
        CancellationToken ct)
    {
        using var whisperFactory = WhisperFactory.FromPath(model.LocalPath);

        var builder = whisperFactory.CreateBuilder()
            .WithThreads(Environment.ProcessorCount);

        if (string.IsNullOrEmpty(language) || language.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            builder = builder.WithLanguageDetection();
        }
        else
        {
            builder = builder.WithLanguage(language);
        }

        using var processor = builder.Build();

        var segments = new List<TranscriptionSegment>();
        string detectedLanguage = language ?? "auto";

        await using var fileStream = File.OpenRead(wavFilePath);

        // Whisper.net reports progress 0-100 via the ProcessAsync progress event.
        await foreach (var result in processor.ProcessAsync(fileStream, ct))
        {
            segments.Add(new TranscriptionSegment(result.Start, result.End, result.Text.Trim()));
            if (!string.IsNullOrEmpty(result.Language))
            {
                detectedLanguage = result.Language;
            }
        }

        var plainText = string.Join(' ', segments.Select(s => s.Text)).Trim();
        var srt = BuildSrt(segments);
        var json = BuildJson(detectedLanguage, model.Name, hardware.DisplayName, segments);

        progress.Report(100);

        return new TranscriptionOutcome(detectedLanguage, plainText, srt, json, segments);
    }

    private static string BuildSrt(List<TranscriptionSegment> segments)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < segments.Count; i++)
        {
            var s = segments[i];
            sb.AppendLine((i + 1).ToString());
            sb.AppendLine($"{Fmt(s.Start)} --> {Fmt(s.End)}");
            sb.AppendLine(s.Text);
            sb.AppendLine();
        }
        return sb.ToString();

        static string Fmt(TimeSpan t) => t.ToString(@"hh\:mm\:ss\,fff");
    }

    private static string BuildJson(string language, string modelName, string hardware, List<TranscriptionSegment> segments)
    {
        var payload = new
        {
            language,
            model = modelName,
            hardware,
            generatedAtUtc = DateTime.UtcNow,
            segments = segments.Select(s => new
            {
                start = s.Start.TotalSeconds,
                end = s.End.TotalSeconds,
                text = s.Text
            })
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }
}
