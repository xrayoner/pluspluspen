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

## Update System (v0.1.0+)

### Installation & Distribution

1. **Build the Release**:
   ```powershell
   cd src/PlusPlusPen
   dotnet publish -c Release -o bin/Release/net8.0-windows/publish
   ```

2. **Create Update Package** (PowerShell):
   ```powershell
   ./tools/CreateUpdatePackage.ps1 -Version "0.1.1" -Notes "Bug fixes and improvements"
   ```
   This creates `update/packages/pluspluspen_update_0.1.1.zip` with:
   - `manifest.json` inside the ZIP
   - All published binaries and dependencies
   - Calculates and outputs SHA256 hash

3. **Upload to GitHub Releases**:
   - Go to [GitHub Releases](https://github.com/xrayoner/pluspluspen/releases)
   - Create a new release with tag `v0.1.1`
   - Upload the ZIP file
   - Copy download URL

4. **Update `update/latest.json`**:
   ```json
   {
     "version": "0.1.1",
     "minVersion": "0.1.0",
     "notes": "Bug fixes and improvements",
     "downloadUrl": "https://github.com/xrayoner/pluspluspen/releases/download/v0.1.1/pluspluspen_update_0.1.1.zip",
     "sha256": "<hash from CreateUpdatePackage.ps1>"
   }
   ```
   Push this to the repository.

### Update Check (Settings > Updates Tab)

**Current Version**: v0.1.0 (shown in the Updates tab)

**Check from Internet** (`İnternetten al`):
- Fetches `latest.json` from: `https://raw.githubusercontent.com/xrayoner/pluspluspen/main/update/latest.json`
- Compares version with current (v0.1.0)
- Shows message: "Yeni sürüm bulundu" or "Güncelleştirme yok"
- Displays: version, minVersion, notes fields

**Load from File** (`Dosya ile al`):
- User selects a ZIP package (e.g., `pluspluspen_update_0.1.1.zip`)
- Reads `manifest.json` from inside the ZIP
- Displays: version, minVersion, notes fields
- Shows "Paket geçerli" message on validation

**Note**: Download and actual installation not yet implemented (test-only validation mode).
