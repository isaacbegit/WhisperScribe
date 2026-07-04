// NOTE: We deliberately use a thin ADO.NET wrapper (Microsoft.Data.Sqlite) rather than full EF Core.
// This keeps the sample dependency-light and makes the schema explicit and easy to audit/extend.
// The actual data-access logic lives in Services/DatabaseService.cs — this file documents the schema.
namespace WhisperScribe.Data;

/// <summary>
/// SQLite schema reference for WhisperScribe.
/// Table: Transcriptions — one row per fully or partially processed audio file.
/// </summary>
public static class Schema
{
    public const string CreateTranscriptionsTable = """
        CREATE TABLE IF NOT EXISTS Transcriptions (
            Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
            FileName           TEXT NOT NULL,
            SourcePath         TEXT NOT NULL,
            FileSizeBytes      INTEGER NOT NULL,
            DurationSeconds    REAL NOT NULL DEFAULT 0,
            Language           TEXT,
            ModelUsed          TEXT,
            HardwareUsed       TEXT,
            CreatedAtUtc       TEXT NOT NULL,
            ProcessingSeconds  REAL NOT NULL DEFAULT 0,
            PlainText          TEXT,
            SrtText            TEXT,
            JsonText           TEXT,
            Status             TEXT NOT NULL DEFAULT 'Completed'
        );
        """;

    public const string CreateSettingsTable = """
        CREATE TABLE IF NOT EXISTS AppSettings (
            Key   TEXT PRIMARY KEY,
            Value TEXT
        );
        """;
}
