using System.IO;
using System.Net.Http;
using WhisperScribe.Models;

namespace WhisperScribe.Services;

/// <summary>
/// Lists the official ggml Whisper models, tracks which are already downloaded locally,
/// and downloads new ones with live progress (used by the Settings tab and to gate
/// "Convert" in the Transcript tab until a model is ready).
///
/// Layout on disk: {ModelsFolder}\{model.Name}\{model.FileName}
/// e.g. C:\Users\you\AppData\Local\WhisperScribe\Models\base.en\ggml-base.en.bin
/// Each model gets its own subfolder under a single configurable root path.
/// </summary>
public class WhisperModelManager
{
    private const string HfBase = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main";
    private readonly HttpClient _http = new();

    /// <summary>The single root folder under which every model gets its own subfolder.</summary>
    public string ModelsFolder { get; private set; }

    public WhisperModelManager(string modelsFolder)
    {
        ModelsFolder = modelsFolder;
        Directory.CreateDirectory(ModelsFolder);
    }

    /// <summary>
    /// Changes the root download folder. Does not move already-downloaded models —
    /// they simply won't show as downloaded anymore until re-fetched into the new location.
    /// </summary>
    public void SetModelsFolder(string newFolder)
    {
        if (string.IsNullOrWhiteSpace(newFolder)) return;
        ModelsFolder = newFolder;
        Directory.CreateDirectory(ModelsFolder);
    }

    private string GetModelSubfolder(WhisperModelInfo model) => Path.Combine(ModelsFolder, model.Name);
    private string GetModelFilePath(WhisperModelInfo model) => Path.Combine(GetModelSubfolder(model), model.FileName);

    public List<WhisperModelInfo> GetCatalog()
    {
        var catalog = new List<WhisperModelInfo>
        {
            Make("tiny",        "Tiny (Multilingual)",        "ggml-tiny.bin",        77_700_000,  "Fastest, lowest accuracy. Great for quick drafts."),
            Make("tiny.en",     "Tiny (English only)",        "ggml-tiny.en.bin",     77_700_000,  "Fastest English-only variant."),
            Make("base",        "Base (Multilingual)",        "ggml-base.bin",        148_000_000, "Good balance of speed and accuracy for most uses."),
            Make("base.en",     "Base (English only)",        "ggml-base.en.bin",     148_000_000, "Recommended default for English content."),
            Make("small",       "Small (Multilingual)",       "ggml-small.bin",       488_000_000, "Solid accuracy, moderate resource use."),
            Make("small.en",    "Small (English only)",       "ggml-small.en.bin",    488_000_000, "Higher accuracy for English-only workloads."),
            Make("medium",      "Medium (Multilingual)",      "ggml-medium.bin",      1_530_000_000, "High accuracy, slower — benefits greatly from GPU."),
            Make("medium.en",   "Medium (English only)",      "ggml-medium.en.bin",   1_530_000_000, "High accuracy, English only."),
            Make("large-v3",    "Large v3 (Multilingual)",    "ggml-large-v3.bin",    3_100_000_000, "Best accuracy available; requires a capable GPU for good speed."),
        };

        foreach (var model in catalog)
        {
            var path = GetModelFilePath(model);
            if (File.Exists(path))
            {
                model.IsDownloaded = true;
                model.LocalPath = path;
            }
        }

        return catalog;
    }

    private WhisperModelInfo Make(string name, string display, string fileName, long size, string description) => new()
    {
        Name = name,
        DisplayName = display,
        FileName = fileName,
        DownloadUrl = $"{HfBase}/{fileName}",
        ApproxSizeBytes = size,
        Description = description
    };

    /// <summary>
    /// Downloads a model into its own subfolder under ModelsFolder, reporting 0-100 progress.
    /// </summary>
    public async Task DownloadModelAsync(WhisperModelInfo model, IProgress<double> progress, CancellationToken ct)
    {
        var subfolder = GetModelSubfolder(model);
        Directory.CreateDirectory(subfolder);

        var destination = GetModelFilePath(model);
        var tempPath = destination + ".part";

        using var response = await _http.GetAsync(model.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? model.ApproxSizeBytes;
        await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;
        while ((read = await httpStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            totalRead += read;
            progress.Report(totalBytes > 0 ? (double)totalRead / totalBytes * 100.0 : 0);
        }

        fileStream.Close();
        File.Move(tempPath, destination, overwrite: true);

        model.IsDownloaded = true;
        model.LocalPath = destination;
    }

    public void DeleteModel(WhisperModelInfo model)
    {
        var subfolder = GetModelSubfolder(model);
        if (Directory.Exists(subfolder)) Directory.Delete(subfolder, recursive: true);
        model.IsDownloaded = false;
        model.LocalPath = string.Empty;
    }
}
