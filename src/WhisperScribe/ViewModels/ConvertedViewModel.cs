using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhisperScribe.Models;
using WhisperScribe.Services;

namespace WhisperScribe.ViewModels;

public partial class ConvertedViewModel : ObservableObject
{
    private readonly DatabaseService _db = App.Database;
    private readonly ExportService _export = App.Export;

    public ObservableCollection<TranscriptionRecord> Records { get; } = new();

    [ObservableProperty] private TranscriptionRecord? _selectedRecord;
    [ObservableProperty] private string _searchText = string.Empty;

    public ConvertedViewModel()
    {
        Load();
    }

    public void Load()
    {
        Records.Clear();
        foreach (var record in _db.GetAll())
            Records.Add(record);
    }

    partial void OnSearchTextChanged(string value)
    {
        Records.Clear();
        var all = _db.GetAll();
        var filtered = string.IsNullOrWhiteSpace(value)
            ? all
            : all.Where(r => r.FileName.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                              r.Language.Contains(value, StringComparison.OrdinalIgnoreCase));
        foreach (var r in filtered) Records.Add(r);
    }

    [RelayCommand]
    private void Refresh() => Load();

    [RelayCommand]
    private void DeleteRecord(TranscriptionRecord? record)
    {
        record ??= SelectedRecord;
        if (record is null) return;
        _db.Delete(record.Id);
        Records.Remove(record);
    }

    [RelayCommand]
    private void ExportText(TranscriptionRecord? record)
    {
        record ??= SelectedRecord;
        if (record is null) return;
        _export.ExportPlainText(record.PlainText, Path.GetFileNameWithoutExtension(record.FileName));
    }

    [RelayCommand]
    private void ExportSrt(TranscriptionRecord? record)
    {
        record ??= SelectedRecord;
        if (record is null) return;
        _export.ExportSrt(record.SrtText, Path.GetFileNameWithoutExtension(record.FileName));
    }

    [RelayCommand]
    private void ExportJson(TranscriptionRecord? record)
    {
        record ??= SelectedRecord;
        if (record is null) return;
        _export.ExportJson(record.JsonText, Path.GetFileNameWithoutExtension(record.FileName));
    }
}
