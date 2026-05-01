using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using PlusPlusPen.Models;

namespace PlusPlusPen.Services;

public sealed class AppSettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };
    private readonly LogService _logService;

    public AppSettingsService(LogService logService)
    {
        _logService = logService;
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PlusPlusPen");
        Directory.CreateDirectory(root);
        _settingsPath = Path.Combine(root, "settings.json");
    }

    public string SettingsPath => _settingsPath;

    public AppSettingsModel Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettingsModel();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettingsModel>(json, _serializerOptions) ?? new AppSettingsModel();
        }
        catch (Exception ex)
        {
            _logService.LogError("Ayar dosyası okunamadı, varsayılan ayarlar kullanıldı.", ex);
            return new AppSettingsModel();
        }
    }

    public void Save(AppSettingsModel settings)
    {
        var json = JsonSerializer.Serialize(settings, _serializerOptions);
        File.WriteAllText(_settingsPath, json);
        ApplyStartupRegistration(settings.LaunchAtStartup);
    }

    public void Reset()
    {
        Save(new AppSettingsModel());
    }

    private void ApplyStartupRegistration(bool launchAtStartup)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                "Software\\Microsoft\\Windows\\CurrentVersion\\Run",
                writable: true);

            if (key is null)
            {
                return;
            }

            if (launchAtStartup)
            {
                var exePath = Environment.ProcessPath ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    key.SetValue("PlusPlusPen", $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue("PlusPlusPen", false);
            }
        }
        catch (Exception ex)
        {
            _logService.LogError("Başlangıçta çalıştırma ayarı uygulanamadı.", ex);
        }
    }
}
