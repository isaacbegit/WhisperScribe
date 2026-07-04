using System.IO;
using System.Windows;
using WhisperScribe.Data;
using WhisperScribe.Services;

namespace WhisperScribe;

public partial class App : Application
{
    public static string AppDataFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhisperScribe");

    public static string ModelsFolder { get; } = Path.Combine(AppDataFolder, "Models");

    public static string DatabasePath { get; } = Path.Combine(AppDataFolder, "whisperscribe.db");

    // Simple hand-rolled service locator — keeps the sample dependency-free (no extra DI package required).
    public static DatabaseService Database { get; private set; } = null!;
    public static HardwareDetectionService Hardware { get; private set; } = null!;
    public static WhisperModelManager Models { get; private set; } = null!;
    public static TranscriptionService Transcription { get; private set; } = null!;
    public static AudioFileService AudioFiles { get; private set; } = null!;
    public static ExportService Export { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory(AppDataFolder);
        Directory.CreateDirectory(ModelsFolder);

        Database = new DatabaseService(DatabasePath);
        Database.Initialize();

        var savedModelsFolder = Database.GetSetting("ModelsFolder");
        var modelsRoot = string.IsNullOrWhiteSpace(savedModelsFolder) ? ModelsFolder : savedModelsFolder;
        Directory.CreateDirectory(modelsRoot);

        Hardware = new HardwareDetectionService();
        Models = new WhisperModelManager(modelsRoot);
        Transcription = new TranscriptionService(Models);
        AudioFiles = new AudioFileService();
        Export = new ExportService();
    }
}
