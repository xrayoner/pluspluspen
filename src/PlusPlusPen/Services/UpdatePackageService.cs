using System.IO;
using System.IO.Compression;
using System.Text.Json;
using PlusPlusPen.Models;

namespace PlusPlusPen.Services;

public sealed class UpdatePackageService
{
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
            var entry = archive.GetEntry("manifest.json");
            if (entry is null)
            {
                throw new InvalidDataException("Paket içinde manifest.json bulunamadı.");
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var manifest = JsonSerializer.Deserialize<UpdateManifestModel>(json);

            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
            {
                throw new InvalidDataException("manifest.json geçersiz veya eksik.");
            }

            return manifest;
        }
        catch (Exception ex)
        {
            _logService.LogError("Güncelleme paketi okunamadı.", ex);
            throw;
        }
    }
}
