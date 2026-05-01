# CreateUpdatePackage.ps1
# ++PEN için güncelleme paketi oluşturan script

param(
    [string]$Version = "0.1.1",
    [string]$Notes = "Güncelleme paketi"
)

$ErrorActionPreference = "Stop"

# Çalışma dizini ayarla
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$PublishDir = Join-Path $ProjectRoot "src\PlusPlusPen\bin\Release\net8.0-windows\publish"
$UpdateDir = Join-Path $ProjectRoot "update"
$PackageDir = Join-Path $UpdateDir "packages"
$ZipPath = Join-Path $PackageDir "pluspluspen_update_$Version.zip"

Write-Host "++PEN Güncelleme Paketi Oluşturucu" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Sürüm: $Version"
Write-Host "Notlar: $Notes"
Write-Host ""

# 1. Publish işlemi
Write-Host "[1/4] Uygulamayı publish ediliyor..." -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    Remove-Item -Path $PublishDir -Recurse -Force
}

$SourceCsproj = Join-Path $ProjectRoot "src\PlusPlusPen\PlusPlusPen.csproj"
dotnet publish $SourceCsproj -c Release -o $PublishDir
if ($LASTEXITCODE -ne 0) {
    throw "Publish başarısız oldu."
}
Write-Host "✓ Publish tamamlandı" -ForegroundColor Green
Write-Host ""

# 2. Paket dizini oluştur
Write-Host "[2/4] Paket dizini hazırlanıyor..." -ForegroundColor Yellow
if (-not (Test-Path $PackageDir)) {
    New-Item -ItemType Directory -Path $PackageDir | Out-Null
}

# 3. manifest.json oluştur
Write-Host "[3/4] manifest.json oluşturuluyor..." -ForegroundColor Yellow
$ManifestPath = Join-Path $PackageDir "manifest.json"
$ManifestContent = @{
    version = $Version
    minVersion = "0.1.0"
    notes = $Notes
} | ConvertTo-Json

Set-Content -Path $ManifestPath -Value $ManifestContent -Encoding UTF8
Write-Host "✓ manifest.json oluşturuldu" -ForegroundColor Green
Write-Host ""

# 4. ZIP paketi oluştur
Write-Host "[4/4] ZIP paketi oluşturuluyor: $ZipPath" -ForegroundColor Yellow

if (Test-Path $ZipPath) {
    Remove-Item -Path $ZipPath -Force
}

# ZIP oluştur
Add-Type -AssemblyName "System.IO.Compression.FileSystem"
$CompressionLevel = [System.IO.Compression.CompressionLevel]::Optimal

# Publish dosyalarını ZIP'e ekle
$ZipStream = [System.IO.Compression.ZipFile]::Open($ZipPath, [System.IO.Compression.ZipArchiveMode]::Create)

# Manifest.json'u ekle
$ManifestEntry = $ZipStream.CreateEntry("manifest.json")
$ManifestStream = $ManifestEntry.Open()
$ManifestBytes = [System.Text.Encoding]::UTF8.GetBytes($ManifestContent)
$ManifestStream.Write($ManifestBytes, 0, $ManifestBytes.Length)
$ManifestStream.Close()

# Publish dosyalarını ekle
Get-ChildItem -Path $PublishDir -Recurse -File | ForEach-Object {
    $RelativePath = $_.FullName.Substring($PublishDir.Length + 1)
    $Entry = $ZipStream.CreateEntry($RelativePath)
    $EntryStream = $Entry.Open()
    $FileStream = [System.IO.File]::OpenRead($_.FullName)
    $FileStream.CopyTo($EntryStream)
    $EntryStream.Close()
    $FileStream.Close()
}

$ZipStream.Close()

Write-Host "✓ ZIP paketi oluşturuldu: $ZipPath" -ForegroundColor Green
Write-Host ""

# 5. SHA256 hesapla
Write-Host "SHA256 Hash Hesaplanıyor..." -ForegroundColor Yellow
$SHA256Hash = (Get-FileHash -Path $ZipPath -Algorithm SHA256).Hash
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║ PAKET OLUŞTURULDU                                            ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "Sürüm: $Version" -ForegroundColor Green
Write-Host "Notlar: $Notes" -ForegroundColor Green
Write-Host ""
Write-Host "Dosya: $ZipPath" -ForegroundColor Green
Write-Host "SHA256:"
Write-Host $SHA256Hash -ForegroundColor Yellow
Write-Host ""
Write-Host "latest.json'ı güncellemek için kullanın:" -ForegroundColor Cyan
Write-Host "version: $Version"
Write-Host "sha256: $SHA256Hash"
Write-Host ""
