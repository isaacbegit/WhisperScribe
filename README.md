# WhisperScribe

**A local, private, AI-powered audio-to-text studio for Windows — built on .NET 10 / WPF.**

No cloud upload, no subscription: your audio never leaves the machine. Transcription runs
entirely on-device using OpenAI's Whisper model via `Whisper.net` (a native wrapper around
`whisper.cpp`), with automatic CPU/GPU acceleration and a modern desktop UI.

> Other name options considered: *EchoScript*, *Vocalis*, *ScribeWave*, *AuralInk*. **WhisperScribe**
> was chosen because it's immediately descriptive (Whisper model + transcription/"scribe"),
> easy to say, and unclaimed-feeling as a product name.

---

## ✨ Features

- **Tab 1 — Transcript**: drag in one or many audio files, see size/duration/detected language
  live in a grid, pick hardware (CPU or detected GPU) + language + Whisper model, convert one
  or all files, watch per-file and overall progress, then preview **Text / SRT / JSON** output
  side-by-side with one-click export.
- **Tab 2 — Converted**: every transcription ever produced, persisted in a local **SQLite**
  database, searchable, with full detail view and re-export.
- **Tab 3 — Settings**: browse the official ggml Whisper model catalog (tiny → large-v3),
  download with a live progress bar (mirrored in the status bar), remove models to reclaim
  disk space. The Transcript tab automatically disables conversion until a model is ready.
- **Automatic hardware detection**: enumerates your GPU(s) via WMI; if an NVIDIA GPU is found,
  `Whisper.net.Runtime.Cuda` is used for accelerated inference, otherwise it falls back to CPU.
- **Modern, frameless UI** in the requested brand palette (`#090040 · #471396 · #B13BFF · #FFCC00`),
  custom title bar, pill-tabs, gradient buttons, card-based layout.

## 🎨 Design system

| Token | Hex | Used for |
|---|---|---|
| Midnight Ink | `#090040` | App background / deep base |
| Royal Grape | `#471396` | Primary panels, gradient start |
| Electric Orchid | `#B13BFF` | Accents, hover states, gradient end |
| Solar Amber | `#FFCC00` | Progress bars, export CTAs, active-tab indicator |

All colors live in `Themes/ColorPalette.xaml`; every control (buttons, tabs, DataGrid, ComboBox,
ProgressBar, ScrollBar) is fully re-templated in `Themes/ControlStyles.xaml` — nothing uses the
stock Windows look.

## 🗂️ Project structure

```
WhisperScribe.sln
src/WhisperScribe/
  App.xaml(.cs)                 – startup, service wiring, AppData/SQLite paths
  MainWindow.xaml(.cs)          – frameless shell, custom title bar, 3-tab nav
  Views/                        – TranscriptView, ConvertedView, SettingsView (XAML + code-behind)
  ViewModels/                   – MainViewModel + one VM per tab (CommunityToolkit.Mvvm)
  Models/                       – AudioFileItem, TranscriptionRecord, WhisperModelInfo, HardwareOption
  Services/
    DatabaseService.cs          – SQLite schema + CRUD
    HardwareDetectionService.cs – CPU/GPU enumeration (WMI)
    WhisperModelManager.cs      – model catalog + download w/ progress
    TranscriptionService.cs     – Whisper.net inference, SRT/JSON building
    AudioFileService.cs         – file probing + transcode to 16kHz mono WAV (NAudio)
    ExportService.cs            – Save-As for txt/srt/json
  Data/AppDbContext.cs          – schema documentation (SQL DDL)
  Themes/                       – ColorPalette.xaml, ControlStyles.xaml
  Converters/                   – value converters for bindings
  Assets/app.ico
```

## 🛠️ Building

**Requirements**: Windows 10/11, [.NET 10 SDK](https://dotnet.microsoft.com/download) with the
`WindowsDesktop` workload, Visual Studio 2022 (17.12+) or `dotnet` CLI.

```bash
git clone <this-folder>
cd WhisperScribe
dotnet restore
dotnet build -c Release
dotnet run --project src/WhisperScribe
```

On first run, go to **Settings** and download a model (start with **Base (English)** — good
accuracy/speed balance). Once downloaded, the **Transcript** tab unlocks conversion.

> ⚠️ This solution was authored and packaged in a Linux sandbox for delivery, since WPF only
> targets Windows — it has not been compiled here. Open it in Visual Studio / `dotnet build`
> on Windows to build and run. The architecture, APIs (`Whisper.net`, `Microsoft.Data.Sqlite`,
> `NAudio`), and XAML are all written against their real, current public APIs, but do a build
> pass and fix any package-version drift before shipping.

## 🚀 Recommended enhancements

1. **Live waveform + word-level timestamps** — Whisper.net exposes per-token timing; render a
   scrubbable waveform (NAudio `WaveFormRenderer`) with a karaoke-style highlight synced to playback.
2. **Batch queue prioritization & pause/resume** — let users reorder the queue and pause a long
   `large-v3` job without losing progress.
3. **Speaker diarization** — pair with a lightweight diarization model (e.g. `pyannote` via a
   sidecar process) to label "Speaker 1 / Speaker 2" in transcripts and SRT.
4. **In-app audio player synced to transcript** — click any sentence to jump playback to that
   timestamp (mirrors the "trailer window" pattern from your CineVault project).
5. **Translation pass** — Whisper natively supports "translate to English"; add a toggle next to
   the language dropdown.
6. **Global search across all transcripts** (Tab 2) with full-text SQLite (`FTS5`) instead of
   `LIKE` filtering, for instant search across hundreds of files.
7. **Drag-and-drop onto the DataGrid**, plus folder-watch (auto-queue new files dropped into a
   "Inbox" folder) for hands-free bulk transcription.
8. **Export presets**: WebVTT, DOCX transcript with speaker headers, and a "copy to clipboard"
   quick action, in addition to TXT/SRT/JSON.
9. **Model benchmark helper** in Settings — run a 10-second sample clip through each downloaded
   model so users can compare speed/accuracy on their own hardware before committing to a run.
10. **Light/dark toggle** — keep the same brand palette but offer a light-surface variant for
    users who prefer it, driven by swapping the `ColorPalette.xaml` dictionary at runtime.
