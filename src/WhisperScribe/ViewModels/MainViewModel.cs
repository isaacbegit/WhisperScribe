using CommunityToolkit.Mvvm.ComponentModel;

namespace WhisperScribe.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public TranscriptViewModel Transcript { get; }
    public ConvertedViewModel Converted { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private string _appVersion = "1.0.0";

    public MainViewModel()
    {
        Transcript = new TranscriptViewModel();
        Converted = new ConvertedViewModel();
        Settings = new SettingsViewModel();

        // Keep the Transcript tab's "model ready" gate in sync with Settings tab downloads.
        Settings.CatalogChanged += (_, _) => Transcript.RefreshModels();

        // Refresh the Converted tab whenever the user switches to it, so newly transcribed
        // files from Tab 1 show up immediately.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedTabIndex) && SelectedTabIndex == 1)
            {
                Converted.Load();
            }
        };
    }
}
