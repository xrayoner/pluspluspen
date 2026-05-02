# ++PEN

++PEN is a .NET 8 WPF overlay drawing tool for Windows 10/11. It opens as a compact floating toolbar and expands into a screen drawing layer for pen and eraser input.

## +P+ 26.1

- Çizim modunda PDF'ye veya masaüstüne tıklayınca toolbar'ın arkaya düşme sorunu düzeltildi.

## Features

- Compact floating toolbar
- Full-screen drawing overlay
- Pen, eraser, undo, redo, save
- Dynamic stroke width and smoothing
- PNG export
- JSON-based settings in `%AppData%\PlusPlusPen\settings.json`
- Separate `++PEN Güncelleştirme Merkezi` updater application

## Project Structure

- `src/PlusPlusPen`: main WPF application
- `src/PlusPlusPen.Updater`: updater application
- `tools/CreateUpdatePackage.ps1`: package creation script
- `update/latest.json`: GitHub-compatible update feed sample

## Build

```powershell
dotnet build PlusPlusPen.sln
```

Run the main app:

```powershell
dotnet run --project src/PlusPlusPen
```

Release publish:

```powershell
dotnet publish src/PlusPlusPen/PlusPlusPen.csproj -c Release
```

## Windows Installer

Installer script:

- `tools/installer/PlusPlusPenSetup.iss`

Installer source folder:

- `src/PlusPlusPen/bin/Release/net8.0-windows/publish`

Build the installer after publish:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "tools\installer\PlusPlusPenSetup.iss"
```

## Update Center

Default feed URL:

`https://raw.githubusercontent.com/xrayoner/pluspluspen/main/update/latest.json`

Example `update/latest.json`:

```json
{
  "App": "++PEN",
  "Version": "+P+ 26.1",
  "MinVersion": "0.1.0",
  "Notes": "Toolbar'ın çizim modunda arkaya düşme sorunu düzeltildi.",
  "DownloadUrl": "https://github.com/xrayoner/pluspluspen/releases/download/26.1/pluspluspen_update_26.1.zip",
  "Sha256": ""
}
```

## Create an Update Package

```powershell
./tools/CreateUpdatePackage.ps1 -Version "26.1" -DisplayVersion "+P+ 26.1" -MinVersion "0.1.0" -Notes "Toolbar'ın çizim modunda arkaya düşme sorunu düzeltildi."
```

The script:

- publishes `PlusPlusPen`
- publishes `PlusPlusPen.Updater`
- copies updater files into the release folder for normal distribution
- creates `update/packages/pluspluspen_update_<version>.zip`
- prints the ZIP SHA256 hash

## Updater Paths

- App settings: `%AppData%\PlusPlusPen\settings.json`
- Main app log: `%AppData%\PlusPlusPen\logs\pluspluspen.log`
- Updater log: `%AppData%\PlusPlusPen\logs\updater.log`
