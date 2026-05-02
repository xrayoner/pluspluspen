namespace PlusPlusPen.Updater.Models;

public sealed class UpdaterOptions
{
    public string PackagePath { get; init; } = string.Empty;

    public string InstallDirectory { get; init; } = string.Empty;

    public string AppExecutablePath { get; init; } = string.Empty;

    public int MainProcessId { get; init; }

    public string CurrentVersion { get; init; } = string.Empty;

    public string ExpectedSha256 { get; init; } = string.Empty;

    public static UpdaterOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length)
            {
                break;
            }

            values[args[index]] = args[index + 1];
        }

        return new UpdaterOptions
        {
            PackagePath = values.GetValueOrDefault("--package", string.Empty),
            InstallDirectory = values.GetValueOrDefault("--install-dir", string.Empty),
            AppExecutablePath = values.GetValueOrDefault("--app-exe", string.Empty),
            CurrentVersion = values.GetValueOrDefault("--current-version", string.Empty),
            ExpectedSha256 = values.GetValueOrDefault("--expected-sha256", string.Empty),
            MainProcessId = int.TryParse(values.GetValueOrDefault("--main-pid", "0"), out var pid) ? pid : 0
        };
    }
}
