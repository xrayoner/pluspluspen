using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using PlusPlusPen.Models;

namespace PlusPlusPen.Services;

public sealed class UpdatePackageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly LogService _logService;

    public UpdatePackageService(LogService logService)
    {
        _logService = logService;
    }

    public UpdateManifestModel ReadManifestFromZip(string zipPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            ValidateArchiveStructure(archive);
            var manifestEntry = archive.GetEntry("manifest.json")
                ?? throw new InvalidDataException("Paket içinde manifest.json bulunamadı.");

            using var stream = manifestEntry.Open();
            var manifest = JsonSerializer.Deserialize<UpdateManifestModel>(stream, JsonOptions)
                ?? throw new InvalidDataException("manifest.json okunamadı.");

            ValidateManifest(manifest);
            return manifest;
        }
        catch (Exception ex)
        {
            _logService.LogError("Güncelleme paketi okunamadı.", ex);
            throw;
        }
    }

    public async Task<UpdateManifestModel> FetchLatestFromUrlAsync(string feedUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(feedUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var manifest = await JsonSerializer.DeserializeAsync<UpdateManifestModel>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidDataException("latest.json okunamadı.");

            ValidateManifest(manifest);
            return manifest;
        }
        catch (Exception ex)
        {
            _logService.LogError("Güncelleme bilgisi indirilemedi.", ex);
            throw;
        }
    }

    public async Task<string> DownloadPackageAsync(UpdateManifestModel manifest, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
        {
            throw new InvalidDataException("İndirme bağlantısı bulunamadı.");
        }

        try
        {
            var updatesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PlusPlusPen",
                "downloads");
            Directory.CreateDirectory(updatesRoot);

            var fileName = Path.GetFileName(new Uri(manifest.DownloadUrl).AbsolutePath);
            if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                fileName = $"pluspluspen_update_{NormalizeVersion(manifest.Version)}.zip";
            }

            var destinationPath = Path.Combine(updatesRoot, fileName);

            using var client = new HttpClient();
            using var response = await client.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using (var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var fileStream = File.Create(destinationPath))
            {
                await downloadStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(manifest.Sha256))
            {
                ValidateSha256(destinationPath, manifest.Sha256);
            }

            _logService.LogInfo($"Güncelleme paketi indirildi: {destinationPath}");
            return destinationPath;
        }
        catch (Exception ex)
        {
            _logService.LogError("Güncelleme paketi indirilemedi.", ex);
            throw;
        }
    }

    public void ValidatePackageForInstall(string zipPath, string currentVersion, string? expectedSha256 = null)
    {
        var manifest = ReadManifestFromZip(zipPath);
        ValidateVersionCompatibility(manifest, currentVersion);

        var shaToValidate = string.IsNullOrWhiteSpace(expectedSha256) ? manifest.Sha256 : expectedSha256;
        if (!string.IsNullOrWhiteSpace(shaToValidate))
        {
            ValidateSha256(zipPath, shaToValidate);
        }
    }

    public void ValidateVersionCompatibility(UpdateManifestModel manifest, string currentVersion)
    {
        if (!IsNewerVersion(manifest.Version, currentVersion))
        {
            throw new InvalidOperationException("Seçilen paket mevcut sürümden yeni değil.");
        }

        if (!IsCurrentVersionCompatible(manifest.MinVersion, currentVersion))
        {
            throw new InvalidOperationException($"Bu paket en az v{NormalizeVersion(manifest.MinVersion)} sürümünü gerektiriyor.");
        }
    }

    public bool IsNewerVersion(string candidateVersion, string currentVersion)
    {
        return CompareVersions(candidateVersion, currentVersion) > 0;
    }

    public bool IsCurrentVersionCompatible(string minVersion, string currentVersion)
    {
        return CompareVersions(currentVersion, minVersion) >= 0;
    }

    public void ValidateSha256(string filePath, string expectedSha256)
    {
        var actual = ComputeSha256(filePath);
        if (!string.Equals(actual, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("SHA256 doğrulaması başarısız oldu.");
        }
    }

    public string ComputeSha256(string filePath)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash).ToUpperInvariant();
        }
        catch (Exception ex)
        {
            _logService.LogError("Dosya SHA256 hesaplanamadı.", ex);
            throw;
        }
    }

    public static string NormalizeVersion(string version)
    {
        return version.Trim().TrimStart('v', 'V');
    }

    public static string FormatDisplayVersion(string version)
    {
        return $"v{NormalizeVersion(version)}";
    }

    private static int CompareVersions(string left, string right)
    {
        var leftParts = NormalizeVersion(left).Split('.', StringSplitOptions.RemoveEmptyEntries);
        var rightParts = NormalizeVersion(right).Split('.', StringSplitOptions.RemoveEmptyEntries);
        var length = Math.Max(leftParts.Length, rightParts.Length);

        for (var index = 0; index < length; index++)
        {
            var leftValue = index < leftParts.Length && int.TryParse(leftParts[index], out var parsedLeft) ? parsedLeft : 0;
            var rightValue = index < rightParts.Length && int.TryParse(rightParts[index], out var parsedRight) ? parsedRight : 0;
            var comparison = leftValue.CompareTo(rightValue);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    private static void ValidateManifest(UpdateManifestModel manifest)
    {
        if (!string.Equals(manifest.App, "++PEN", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(manifest.App, "PlusPlusPen", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Paket uygulama adı ++PEN değil.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version)
            || string.IsNullOrWhiteSpace(manifest.MinVersion)
            || string.IsNullOrWhiteSpace(manifest.Notes))
        {
            throw new InvalidDataException("manifest.json içinde zorunlu alanlar eksik.");
        }
    }

    private static void ValidateArchiveStructure(ZipArchive archive)
    {
        var hasAppContent = false;

        foreach (var entry in archive.Entries)
        {
            var path = entry.FullName.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (path.StartsWith('/') || Path.IsPathRooted(path))
            {
                throw new InvalidDataException("ZIP içinde mutlak yol tespit edildi.");
            }

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment == ".."))
            {
                throw new InvalidDataException("ZIP içinde geçersiz yol bulundu.");
            }

            if (string.Equals(path, "manifest.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (path.StartsWith("app/", StringComparison.OrdinalIgnoreCase))
            {
                hasAppContent = true;
                continue;
            }

            throw new InvalidDataException("Paket yapısında yalnızca manifest.json ve app/ klasörü bulunabilir.");
        }

        if (!hasAppContent)
        {
            throw new InvalidDataException("Paket içinde app/ klasörü bulunamadı.");
        }
    }
}
