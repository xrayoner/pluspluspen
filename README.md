# ++PEN

++PEN is a .NET 8 WPF desktop overlay drawing tool for Windows 10/11. It opens as a compact floating toolbar and expands into a transparent full-screen drawing layer when the pen or eraser is active.

## MVP Features

- Draggable floating toolbar with translucent light-blue theme
- Logo click opens settings
- Full-screen transparent overlay that covers the entire virtual desktop, including taskbar area
- Pen, eraser, undo, redo, save, color palette, and thickness presets
- Green active state for the selected pen tool
- Fountain-pen-like dynamic stroke width
- Optional smoothing and dynamic thickness toggles
- Transparent PNG export with automatic file name: `pluspluspen_YYYYMMDD_HHMMSS.png`
- Multi-tab settings window for general, theme, pen, eraser, save, updates, and maintenance
- JSON-based persistent settings stored at `%AppData%\PlusPlusPen\settings.json`
- ZIP update package inspection that reads `manifest.json`
- Basic logging at `%AppData%\PlusPlusPen\logs\pluspluspen.log`

## Project Structure

- `src/PlusPlusPen/Controls`: reusable drawing surface control
- `src/PlusPlusPen/Models`: drawing and settings models
- `src/PlusPlusPen/Services`: overlay, persistence, export, and history services
- `src/PlusPlusPen/ViewModels`: MVVM view models and commands
- `src/PlusPlusPen/Views`: toolbar, overlay, and settings windows

## Build

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK with WPF workload support

Commands:

```powershell
cd src/PlusPlusPen
dotnet restore
dotnet build
dotnet run
```

## Notes

- The current environment used for authoring did not include a .NET SDK, so the project files were prepared carefully but could not be compiled in-place here.
- Overlay sizing uses the Windows virtual desktop metrics so drawing can extend across the full visible desktop region.
- Settings are stored under `%AppData%\PlusPlusPen\settings.json`.
- Broken settings files are ignored safely and the app falls back to defaults.
- The "Internet update" flow is still a placeholder; "Load from File" currently validates and previews update manifests only.
