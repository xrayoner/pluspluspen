param(
    [string]$Version = "2.1",
    [string]$MinVersion = "0.1.0",
    [string]$Notes = "Aktif mod mavi çerçeve düzeltildi, silgi geçiş ve silme bugları düzeltildi, silgi kullanımı iyileştirildi."
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$mainProject = Join-Path $projectRoot "src\PlusPlusPen\PlusPlusPen.csproj"
$updaterProject = Join-Path $projectRoot "src\PlusPlusPen.Updater\PlusPlusPen.Updater.csproj"
$assetSourceDir = Join-Path $projectRoot "src\PlusPlusPen\Assets"

$artifactsRoot = Join-Path $projectRoot "artifacts"
$mainPublishDir = Join-Path $artifactsRoot "publish\PlusPlusPen"
$updaterPublishDir = Join-Path $artifactsRoot "publish\PlusPlusPen.Updater"
$packageWorkDir = Join-Path $artifactsRoot "package-work"
$updateDir = Join-Path $projectRoot "update"
$packageDir = Join-Path $updateDir "packages"
$zipPath = Join-Path $packageDir "pluspluspen_update_$Version.zip"

Write-Host "++PEN Guncelleme Paketi Olusturucu" -ForegroundColor Cyan
Write-Host "Surum: $Version" -ForegroundColor Cyan
Write-Host ""

foreach ($path in @($mainPublishDir, $updaterPublishDir, $packageWorkDir)) {
    if (Test-Path $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $mainPublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $updaterPublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $packageWorkDir -Force | Out-Null
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

Write-Host "[1/5] Ana uygulama publish aliniyor..." -ForegroundColor Yellow
dotnet publish $mainProject -c Release -o $mainPublishDir
if ($LASTEXITCODE -ne 0) { throw "Ana uygulama publish basarisiz oldu." }

Write-Host "[2/5] Guncellestirme Merkezi publish aliniyor..." -ForegroundColor Yellow
dotnet publish $updaterProject -c Release -o $updaterPublishDir
if ($LASTEXITCODE -ne 0) { throw "Updater publish basarisiz oldu." }

Write-Host "[3/5] Guncellestirme Merkezi ana cikti klasorune kopyalaniyor..." -ForegroundColor Yellow
Get-ChildItem -Path $updaterPublishDir -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $mainPublishDir $_.Name) -Force
}

$manifestPath = Join-Path $packageWorkDir "manifest.json"
$manifest = [ordered]@{
    App = "++PEN"
    Version = $Version
    MinVersion = $MinVersion
    Notes = $Notes
    CreatedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssK")
    DownloadUrl = ""
    Sha256 = ""
} | ConvertTo-Json -Depth 4

Set-Content -LiteralPath $manifestPath -Value $manifest -Encoding UTF8

$appDir = Join-Path $packageWorkDir "app"
New-Item -ItemType Directory -Path $appDir -Force | Out-Null

Write-Host "[4/5] ZIP calisma klasoru hazirlaniyor..." -ForegroundColor Yellow
Get-ChildItem -Path $mainPublishDir -File | Where-Object {
    $_.Name -notlike "PlusPlusPen.Updater*"
} | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $appDir $_.Name) -Force
}

Get-ChildItem -Path $mainPublishDir -Directory | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $appDir $_.Name) -Recurse -Force
}

if (Test-Path $assetSourceDir) {
    Copy-Item -LiteralPath $assetSourceDir -Destination (Join-Path $appDir "Assets") -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Write-Host "[5/5] ZIP olusturuluyor..." -ForegroundColor Yellow
Compress-Archive -Path (Join-Path $packageWorkDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$sha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash

Write-Host ""
Write-Host "Paket hazirlandi:" -ForegroundColor Green
Write-Host $zipPath -ForegroundColor Green
Write-Host ""
Write-Host "SHA256:" -ForegroundColor Yellow
Write-Host $sha256 -ForegroundColor Yellow
Write-Host ""
Write-Host "latest.json ornegi:" -ForegroundColor Cyan
Write-Host "{"
Write-Host "  `"App`": `"++PEN`","
Write-Host "  `"Version`": `"$Version`","
Write-Host "  `"MinVersion`": `"$MinVersion`","
Write-Host "  `"Notes`": `"$Notes`","
Write-Host "  `"DownloadUrl`": `"https://github.com/xrayoner/pluspluspen/releases/download/v$Version/pluspluspen_update_$Version.zip`","
Write-Host "  `"Sha256`": `"$sha256`""
Write-Host "}"
