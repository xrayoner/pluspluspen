namespace PlusPlusPen.Updater.Models;

public sealed class UpdateManifestModel
{
    public string App { get; set; } = "++PEN";

    public string Version { get; set; } = string.Empty;

    public string MinVersion { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public string DownloadUrl { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;
}
