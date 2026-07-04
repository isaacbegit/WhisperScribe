using Microsoft.Win32;
using System.IO;

namespace WhisperScribe.Services;

/// <summary>
/// Handles "Export" actions for the Text / SRT / JSON tabs in the Transcript view.
/// </summary>
public class ExportService
{
    public void ExportText(string content, string suggestedFileName, string filter, string defaultExt)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = filter,
            DefaultExt = defaultExt
        };

        if (dialog.ShowDialog() == true)
        {
            File.WriteAllText(dialog.FileName, content);
        }
    }

    public void ExportPlainText(string content, string baseFileName) =>
        ExportText(content, $"{baseFileName}.txt", "Text files (*.txt)|*.txt|All files (*.*)|*.*", ".txt");

    public void ExportSrt(string content, string baseFileName) =>
        ExportText(content, $"{baseFileName}.srt", "SubRip subtitle (*.srt)|*.srt|All files (*.*)|*.*", ".srt");

    public void ExportJson(string content, string baseFileName) =>
        ExportText(content, $"{baseFileName}.json", "JSON files (*.json)|*.json|All files (*.*)|*.*", ".json");
}
