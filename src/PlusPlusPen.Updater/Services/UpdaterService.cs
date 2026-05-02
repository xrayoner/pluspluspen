using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using PlusPlusPen.Updater.Models;

namespace PlusPlusPen.Updater.Services;

public sealed class UpdaterService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly UpdaterLogService _logService;

    public UpdaterService(UpdaterLogService logService)
    {
        _logService = logService;
    }

    public async Task RunAsync(UpdaterOptions options, Action<string> reportStatus)
    {
        string? backupDirectory = null;
        var installedFiles = new List<string>();

        try
        {
            reportStatus("Hazırlanıyor...");
            ValidateOptions(options);

            reportStatus("Paket doğrulanıyor...");
            var manifest = ValidatePackage(options);

            if (options.MainProcessId > 0)
            {
                reportStatus("++PEN kapatılıyor...");
                await WaitForMainProcessAsync(options.MainProcessId).ConfigureAwait(false);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PlusPlusPen",
                "backup");
            var updatesRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PlusPlusPen",
                "updates");
            Directory.CreateDirectory(backupRoot);
            Directory.CreateDirectory(updatesRoot);

            backupDirectory = Path.Combine(backupRoot, $"{NormalizeVersion(manifest.Version)}_{timestamp}");
            var extractDirectory = Path.Combine(updatesRoot, $"{NormalizeVersion(manifest.Version)}_{timestamp}");

            reportStatus("Yedek alınıyor...");
            CopyDirectory(options.InstallDirectory, backupDirectory, overwrite: true);

            reportStatus("Dosyalar güncelleniyor...");
            installedFiles = ExtractAndInstallPackage(options.PackagePath, options.InstallDirectory, extractDirectory);

            reportStatus("Güncelleme tamamlandı.");
            _logService.Info($"Kurulum tamamlandı: {manifest.Version}");

            reportStatus("++PEN yeniden başlatılıyor...");
            StartApplication(options.AppExecutablePath, options.InstallDirectory);
        }
        catch (Exception ex)
        {
            _logService.Error("Güncelleme başarısız oldu.", ex);
            if (!string.IsNullOrWhiteSpace(backupDirectory) && Directory.Exists(backupDirectory))
            {
                try
                {
                    Rollback(options.InstallDirectory, backupDirectory!, installedFiles);
                    _logService.Info("Rollback tamamlandı.");
                }
                catch (Exception rollbackEx)
                {
                    _logService.Error("Rollback başarısız oldu.", rollbackEx);
                }
            }

            throw;
        }
    }

    private void ValidateOptions(UpdaterOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PackagePath) || !File.Exists(options.PackagePath))
        {
            throw new FileNotFoundException("Güncelleme paketi bulunamadı.", options.PackagePath);
        }

        if (string.IsNullOrWhiteSpace(options.InstallDirectory) || !Directory.Exists(options.InstallDirectory))
        {
            throw new DirectoryNotFoundException("Kurulum klasörü bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(options.AppExecutablePath))
        {
            throw new InvalidOperationException("Uygulama yürütülebilir dosyası belirtilmedi.");
        }

        if (string.IsNullOrWhiteSpace(options.CurrentVersion))
        {
            throw new InvalidOperationException("Mevcut sürüm bilgisi belirtilmedi.");
        }
    }

    private UpdateManifestModel ValidatePackage(UpdaterOptions options)
    {
        using var archive = ZipFile.OpenRead(options.PackagePath);
        var entries = GetSafeEntries(archive).ToList();
        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidDataException("manifest.json bulunamadı.");

        using var stream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<UpdateManifestModel>(stream, JsonOptions);
        if (manifest is null)
        {
            throw new InvalidDataException("manifest.json okunamadı.");
        }

        ValidateManifest(manifest);
        ValidateVersionRules(manifest, options.CurrentVersion);

        var expectedSha = string.IsNullOrWhiteSpace(options.ExpectedSha256) ? manifest.Sha256 : options.ExpectedSha256;
        if (!string.IsNullOrWhiteSpace(expectedSha))
        {
            ValidateSha256(options.PackagePath, expectedSha);
        }

        if (entries.Count == 0)
        {
            throw new InvalidDataException("Paket içinde kurulacak dosya bulunamadı.");
        }

        return manifest;
    }

    private List<string> ExtractAndInstallPackage(string packagePath, string installDirectory, string extractDirectory)
    {
        if (Directory.Exists(extractDirectory))
        {
            Directory.Delete(extractDirectory, recursive: true);
        }

        Directory.CreateDirectory(extractDirectory);
        var installedRelativePaths = new List<string>();

        using var archive = ZipFile.OpenRead(packagePath);
        foreach (var entry in GetSafeEntries(archive))
        {
            var relativePath = entry.FullName["app/".Length..].Replace('/', Path.DirectorySeparatorChar);
            var extractedPath = Path.Combine(extractDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(extractedPath)!);
            entry.ExtractToFile(extractedPath, overwrite: true);
        }

        foreach (var file in Directory.GetFiles(extractDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(extractDirectory, file);
            var destinationPath = Path.Combine(installDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
            installedRelativePaths.Add(relativePath);
        }

        return installedRelativePaths;
    }

    private void Rollback(string installDirectory, string backupDirectory, IEnumerable<string> installedFiles)
    {
        foreach (var relativePath in installedFiles)
        {
            var targetPath = Path.Combine(installDirectory, relativePath);
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }

        CopyDirectory(backupDirectory, installDirectory, overwrite: true);
    }

    private static async Task WaitForMainProcessAsync(int mainProcessId)
    {
        try
        {
            using var process = Process.GetProcessById(mainProcessId);
            if (process.HasExited)
            {
                return;
            }

            await Task.Run(() => process.WaitForExit(30000)).ConfigureAwait(false);
            process.Refresh();
            if (!process.HasExited)
            {
                throw new TimeoutException("++PEN beklenen sürede kapanmadı.");
            }
        }
        catch (ArgumentException)
        {
        }
    }

    private static void StartApplication(string appExecutablePath, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = appExecutablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    private static IEnumerable<ZipArchiveEntry> GetSafeEntries(ZipArchive archive)
    {
        foreach (var entry in archive.Entries)
        {
            var path = entry.FullName.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(path) || path.EndsWith('/'))
            {
                continue;
            }

            if (path.StartsWith('/') || Path.IsPathRooted(path))
            {
                throw new InvalidDataException("ZIP içinde mutlak yol bulundu.");
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

            if (!path.StartsWith("app/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Paket içinde yalnızca app/ klasörü kurulabilir.");
            }

            yield return entry;
        }
    }

    private static void ValidateManifest(UpdateManifestModel manifest)
    {
        if (!string.Equals(manifest.App, "++PEN", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(manifest.App, "PlusPlusPen", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Paket uygulama adı geçersiz.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version)
            || string.IsNullOrWhiteSpace(manifest.MinVersion)
            || string.IsNullOrWhiteSpace(manifest.Notes))
        {
            throw new InvalidDataException("manifest.json içinde zorunlu alanlar eksik.");
        }
    }

    private static void ValidateVersionRules(UpdateManifestModel manifest, string currentVersion)
    {
        if (CompareVersions(manifest.Version, currentVersion) <= 0)
        {
            throw new InvalidOperationException("Paket sürümü mevcut sürümden yeni değil.");
        }

        if (CompareVersions(currentVersion, manifest.MinVersion) < 0)
        {
            throw new InvalidOperationException($"Bu paket en az v{NormalizeVersion(manifest.MinVersion)} sürümünü gerektiriyor.");
        }
    }

    private static void ValidateSha256(string packagePath, string expectedSha256)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(packagePath);
        var actual = Convert.ToHexString(sha256.ComputeHash(stream));
        if (!string.Equals(actual, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Paket SHA256 doğrulaması başarısız oldu.");
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(destinationDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite);
        }
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

    private static string NormalizeVersion(string version)
    {
        return version.Trim().TrimStart('v', 'V');
    }
}
