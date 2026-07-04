using Microsoft.Data.Sqlite;
using WhisperScribe.Data;
using WhisperScribe.Models;

namespace WhisperScribe.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = Schema.CreateTranscriptionsTable;
        cmd.ExecuteNonQuery();

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = Schema.CreateSettingsTable;
        cmd2.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public int InsertTranscription(TranscriptionRecord record)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Transcriptions
                (FileName, SourcePath, FileSizeBytes, DurationSeconds, Language, ModelUsed,
                 HardwareUsed, CreatedAtUtc, ProcessingSeconds, PlainText, SrtText, JsonText, Status)
            VALUES
                ($fileName, $sourcePath, $fileSize, $duration, $language, $model,
                 $hardware, $createdAt, $processingSeconds, $plainText, $srtText, $jsonText, $status);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$fileName", record.FileName);
        cmd.Parameters.AddWithValue("$sourcePath", record.SourcePath);
        cmd.Parameters.AddWithValue("$fileSize", record.FileSizeBytes);
        cmd.Parameters.AddWithValue("$duration", record.DurationSeconds);
        cmd.Parameters.AddWithValue("$language", record.Language);
        cmd.Parameters.AddWithValue("$model", record.ModelUsed);
        cmd.Parameters.AddWithValue("$hardware", record.HardwareUsed);
        cmd.Parameters.AddWithValue("$createdAt", record.CreatedAtUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$processingSeconds", record.ProcessingSeconds);
        cmd.Parameters.AddWithValue("$plainText", record.PlainText);
        cmd.Parameters.AddWithValue("$srtText", record.SrtText);
        cmd.Parameters.AddWithValue("$jsonText", record.JsonText);
        cmd.Parameters.AddWithValue("$status", record.Status);

        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    public List<TranscriptionRecord> GetAll()
    {
        var results = new List<TranscriptionRecord>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Transcriptions ORDER BY CreatedAtUtc DESC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(Map(reader));
        }
        return results;
    }

    public TranscriptionRecord? GetById(int id)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Transcriptions WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public void Delete(int id)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Transcriptions WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void SetSetting(string key, string value)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO AppSettings (Key, Value) VALUES ($key, $value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", value);
        cmd.ExecuteNonQuery();
    }

    public string? GetSetting(string key)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Value FROM AppSettings WHERE Key = $key;";
        cmd.Parameters.AddWithValue("$key", key);
        return cmd.ExecuteScalar() as string;
    }

    private static TranscriptionRecord Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("Id")),
        FileName = r.GetString(r.GetOrdinal("FileName")),
        SourcePath = r.GetString(r.GetOrdinal("SourcePath")),
        FileSizeBytes = r.GetInt64(r.GetOrdinal("FileSizeBytes")),
        DurationSeconds = r.GetDouble(r.GetOrdinal("DurationSeconds")),
        Language = r.IsDBNull(r.GetOrdinal("Language")) ? "" : r.GetString(r.GetOrdinal("Language")),
        ModelUsed = r.IsDBNull(r.GetOrdinal("ModelUsed")) ? "" : r.GetString(r.GetOrdinal("ModelUsed")),
        HardwareUsed = r.IsDBNull(r.GetOrdinal("HardwareUsed")) ? "" : r.GetString(r.GetOrdinal("HardwareUsed")),
        CreatedAtUtc = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAtUtc"))),
        ProcessingSeconds = r.GetDouble(r.GetOrdinal("ProcessingSeconds")),
        PlainText = r.IsDBNull(r.GetOrdinal("PlainText")) ? "" : r.GetString(r.GetOrdinal("PlainText")),
        SrtText = r.IsDBNull(r.GetOrdinal("SrtText")) ? "" : r.GetString(r.GetOrdinal("SrtText")),
        JsonText = r.IsDBNull(r.GetOrdinal("JsonText")) ? "" : r.GetString(r.GetOrdinal("JsonText")),
        Status = r.GetString(r.GetOrdinal("Status")),
    };
}
