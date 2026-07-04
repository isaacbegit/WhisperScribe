# 🎙️ WhisperScribe

**A local-first, privacy-focused audio-to-text desktop studio for Windows.**

Built on **.NET 10 / WPF**, WhisperScribe transcribes audio files entirely on your own machine using OpenAI's **Whisper** model (via `Whisper.net`, a native wrapper around `whisper.cpp`) — no cloud upload, no subscription, no API keys. Every transcription is stored locally in **SQLite** and can be exported as **TXT**, **SRT**, or **JSON**.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF-0078D4?logo=windows)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Status](https://img.shields.io/badge/status-active--development-yellow)

---

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Design system](#design-system)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Project structure](#project-structure)
- [Getting started](#getting-started)
- [Usage guide](#usage-guide)
- [Database schema](#database-schema)
- [Configuration](#configuration)
- [Building a release](#building-a-release)
- [Troubleshooting](#troubleshooting)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

---

## Features

### 🎧 Transcript tab
- Browse and queue one or many audio files (`.mp3 .wav .m4a .aac .flac .ogg .wma .opus .mp4 .mkv`).
- Live grid showing file size, duration, detected language, and per-file conversion progress.
- Pick your **compute hardware** (auto-detected CPU thread count, or any GPU found via WMI — NVIDIA cards automatically get CUDA acceleration through `Whisper.net.Runtime.Cuda`).
- Pick a **language** (or Auto-detect) and a **Whisper model** from a dropdown that shows a live **Downloaded / Not downloaded** badge per model.
- A clear warning banner (with guard logic in the view model) stops you from converting until the selected model is actually downloaded.
- **Convert Selected** or **Convert All**, with a per-file progress bar and an overall batch progress bar.
- Right-hand preview panel with **Text / SRT / JSON** tabs, each with its own export button.

### 🗂️ Converted tab
- Every transcription ever produced, persisted locally in SQLite.
- Search by file name or language.
- Detail panel for the selected record with re-export to TXT / SRT / JSON.
- Delete records you no longer need.

### ⚙️ Settings tab
- **Models download path**: choose a single root folder; every model is downloaded into its own subfolder (`{root}\{modelName}\{fileName}`), keeping things tidy. The path is remembered across restarts.
- Browse the full official **ggml model catalog** (`tiny` → `large-v3`, English-only and multilingual variants) with size estimates and descriptions.
- Download any model with a live, full-width progress bar; progress is mirrored in the app's status bar.
- Remove models you no longer need to reclaim disk space.
- The Transcript tab automatically unlocks conversion the moment a model finishes downloading.

### 🎨 Modern UI
- Frameless custom window chrome with a slim drag/title strip.
- Left **sidebar navigation** (Transcript / Converted / Settings) with icon + label items and a left-to-right gradient highlight on the active tab.
- Fully re-templated controls — buttons, tabs, DataGrid, ComboBox, ProgressBar, ScrollBar — none of the stock Windows look.
- Card-based layouts with rounded corners and a consistent navy/blue/green/red color language (see [Design system](#design-system)).

---

## Screenshots

> _Add screenshots or a short GIF of the Transcript, Converted, and Settings tabs here once you have a build — e.g._
>
> `docs/screenshots/transcript.png`, `docs/screenshots/converted.png`, `docs/screenshots/settings.png`

---

## Design system

| Token | Hex | Used for |
|---|---|---|
| Midnight Ink | `#0B1220` | App background |
| Sidebar | `#0F1B2E` | Left navigation panel |
| Primary Blue | `#2F6FED` | Primary buttons, links, progress gradient start |
| Accent Blue | `#4C82F0` | Hover states |
| Success Green | `#22C55E` | "Downloaded" badges, success actions |
| Danger Red | `#EF4444` | Browse Files, Export actions, destructive buttons |
| Warning Amber | `#F5A623` | "Model not downloaded" callouts |
| Selected-tab gradient | `#175BC0 → #0C3B84 → #081C45 → #06163C` (left → right) | Active sidebar item |

All tokens live in `Themes/ColorPalette.xaml`; every control is re-templated in `Themes/ControlStyles.xaml`, so retheming the whole app is a matter of editing those two files.

---

## Tech stack

| Layer | Choice | Why |
|---|---|---|
| UI | WPF (.NET 10, `net10.0-windows`) | Native Windows desktop performance and full XAML control-templating |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) | Source-generated `ObservableProperty` / `RelayCommand`, minimal boilerplate |
| Speech-to-text | [Whisper.net](https://github.com/sandrohanea/whisper.net) (+ `Whisper.net.Runtime.Cuda`) | Local, offline Whisper inference via `whisper.cpp`, with optional GPU acceleration |
| Storage | `Microsoft.Data.Sqlite` | Lightweight, file-based, zero-install local database |
| Audio I/O | [NAudio](https://github.com/naudio/NAudio) | Format probing, decoding, and resampling to the 16kHz mono PCM WAV Whisper requires |
| Hardware detection | `System.Management` (WMI) | Enumerates GPUs to populate the hardware picker |

---

## Architecture

```
UI (Views, XAML)  ──bind──►  ViewModels (CommunityToolkit.Mvvm)  ──call──►  Services  ──►  Data / External APIs
```

- **Views** are dumb: pure XAML bound to view-model properties/commands, no business logic in code-behind (aside from window chrome).
- **ViewModels** (`MainViewModel`, `TranscriptViewModel`, `ConvertedViewModel`, `SettingsViewModel`) hold UI state and orchestrate services.
- **Services** are plain C# classes, each with a single responsibility:
  - `DatabaseService` — SQLite schema + CRUD
  - `HardwareDetectionService` — CPU/GPU enumeration
  - `WhisperModelManager` — model catalog, download, per-model subfolder management
  - `TranscriptionService` — wraps `Whisper.net` inference, builds SRT/JSON
  - `AudioFileService` — probing (size/duration) + transcoding to Whisper-ready WAV
  - `ExportService` — Save-As dialogs for TXT/SRT/JSON
- Services are wired up once in `App.xaml.cs` (a simple hand-rolled service locator — no DI container dependency, kept intentionally lightweight).

---

## Project structure

```
WhisperScribe.sln
src/WhisperScribe/
├── App.xaml(.cs)                 # Startup, AppData/SQLite paths, service wiring
├── MainWindow.xaml(.cs)          # Frameless shell, custom title strip, sidebar nav host
├── Views/
│   ├── TranscriptView.xaml(.cs)  # Tab 1: queue + settings (left) / output preview (right)
│   ├── ConvertedView.xaml(.cs)   # Tab 2: full transcription history
│   └── SettingsView.xaml(.cs)    # Tab 3: models path + model catalog/downloads
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── TranscriptViewModel.cs
│   ├── ConvertedViewModel.cs
│   └── SettingsViewModel.cs
├── Models/
│   ├── AudioFileItem.cs          # Row in the Transcript queue (observable)
│   ├── TranscriptionRecord.cs    # Row as persisted/read from SQLite
│   ├── WhisperModelInfo.cs       # Catalog entry + download state
│   └── HardwareOption.cs         # CPU/GPU compute choice
├── Services/
│   ├── DatabaseService.cs
│   ├── HardwareDetectionService.cs
│   ├── WhisperModelManager.cs
│   ├── TranscriptionService.cs
│   ├── AudioFileService.cs
│   └── ExportService.cs
├── Data/
│   └── AppDbContext.cs           # SQL DDL reference (schema documentation)
├── Themes/
│   ├── ColorPalette.xaml         # All color/brush tokens
│   └── ControlStyles.xaml        # Every control template (buttons, tabs, grid, etc.)
├── Converters/
│   └── Converters.cs             # Value converters used in bindings
└── Assets/
    └── app.ico
```

---

## Getting started

### Prerequisites

- **Windows 10/11**
- [.NET 10 SDK](https://dotnet.microsoft.com/download) with the **Windows Desktop** workload
- Visual Studio 2022 (17.12+) *or* the `dotnet` CLI
- ~5 GB free disk space if you plan to try the larger Whisper models

### Clone & build

```bash
git clone https://github.com/<your-username>/WhisperScribe.git
cd WhisperScribe
dotnet restore
dotnet build -c Release
```

### Run

```bash
dotnet run --project src/WhisperScribe
```

Or open `WhisperScribe.sln` in Visual Studio and press **F5**.

### First launch

1. Go to the **Settings** tab.
2. (Optional) Set your preferred **Models Download Path**.
3. Download a model — **Base (English)** is a good starting point for speed/accuracy balance.
4. Switch to **Transcript**, browse in an audio file, and hit **Convert Selected** or **Convert All**.

---

## Usage guide

### Transcribing a file

1. **Transcript tab → ＋ Browse Files** and pick one or more audio files.
2. Choose **Hardware** (CPU or a detected GPU), **Language** (or leave on Auto-detect), and a **Whisper Model**.
3. Click a row to select it, then **Convert Selected** — or **Convert All** to process every pending file in the queue.
4. Watch the per-row progress bar and the overall batch progress bar.
5. Once done, select the file and use the **Text / SRT / JSON** tabs on the right to preview and export.

### Managing your library

- **Converted tab** lists everything you've ever transcribed. Use the search box to filter by file name or language, click a row to preview it, and export or delete from there.

### Managing models

- **Settings tab** lets you change where models are stored and download/remove individual models. The Transcript tab's model dropdown always reflects current download state with a badge.

---

## Database schema

SQLite database location: `%LocalAppData%\WhisperScribe\whisperscribe.db`

```sql
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

CREATE TABLE IF NOT EXISTS AppSettings (
    Key   TEXT PRIMARY KEY,
    Value TEXT
);
```

`AppSettings` currently stores one key, `ModelsFolder`, so your chosen models path survives a restart.

---

## Configuration

| Setting | Where it lives | Notes |
|---|---|---|
| Models download folder | `AppSettings` table (`ModelsFolder` key), editable from **Settings → Models Download Path** | Each model gets its own subfolder under this root |
| SQLite database | `%LocalAppData%\WhisperScribe\whisperscribe.db` | Created automatically on first run |
| Default models folder | `%LocalAppData%\WhisperScribe\Models` | Used until you pick a custom path |

---

## Building a release

```bash
dotnet publish src/WhisperScribe -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output lands in `src/WhisperScribe/bin/Release/net10.0-windows/win-x64/publish/`. Adjust `-r` (RID) and `--self-contained` to taste depending on whether you want to ship the .NET runtime alongside the app.

---

## Troubleshooting

- **"Convert" buttons stay disabled** — make sure a model shows the green **Downloaded** badge in the dropdown; the buttons unlock automatically once a valid model + at least one queued/selected file are present.
- **GPU not showing up** — GPU detection uses WMI (`Win32_VideoController`); some virtual machines or locked-down environments block WMI queries, in which case the app safely falls back to CPU-only.
- **Download fails partway through** — re-run the download; partial files are written to a `.part` temp file and only renamed into place once complete, so a failed download won't leave a corrupt model file behind.
- **Unsupported audio container** — most common formats are handled via NAudio + Windows Media Foundation; if a file fails to probe or transcode, try converting it to `.wav` or `.mp3` first.

---

## Roadmap

- [ ] Waveform view with word-level timestamp highlighting during playback
- [ ] Pause/resume for long-running batch conversions
- [ ] Speaker diarization (multi-speaker labeling)
- [ ] In-app audio player synced to the transcript
- [ ] Built-in translate-to-English toggle (Whisper supports this natively)
- [ ] Full-text search (SQLite FTS5) across the Converted library
- [ ] Drag-and-drop onto the file queue + folder-watch auto-ingest
- [ ] Additional export presets (WebVTT, DOCX with speaker headers, clipboard copy)
- [ ] In-app model benchmarking to compare speed/accuracy on your own hardware
- [ ] Light theme variant

Contributions toward any of these are very welcome — see below.

---

## Contributing

1. Fork the repo and create a feature branch: `git checkout -b feature/my-feature`
2. Keep changes scoped — one feature or fix per PR is easiest to review.
3. Follow the existing MVVM separation: no business logic in code-behind, no view-specific logic in services.
4. Match the existing style conventions in `Themes/` when adding new controls — reuse existing brush/style keys instead of hardcoding colors.
5. Open a pull request describing what changed and why.

Bug reports and feature requests are welcome via GitHub Issues.

---

## License

Distributed under the **MIT License**. See `LICENSE` for details.

---

## Acknowledgements

- [OpenAI Whisper](https://github.com/openai/whisper) — the underlying speech recognition model
- [whisper.cpp](https://github.com/ggerganov/whisper.cpp) — the fast C/C++ inference engine
- [Whisper.net](https://github.com/sandrohanea/whisper.net) — the .NET bindings this app is built on
- [NAudio](https://github.com/naudio/NAudio) — audio decoding/resampling
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) — MVVM source generators
