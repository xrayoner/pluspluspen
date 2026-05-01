namespace PlusPlusPen.Models;

public sealed class UpdateManifestModel
{
    public string Version { get; set; } = string.Empty;

    public string MinVersion { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
