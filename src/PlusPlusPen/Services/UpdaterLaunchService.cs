using System.Diagnostics;
using System.IO;
using System.Text;

namespace PlusPlusPen.Services;

public sealed class UpdaterLaunchService
{
    private readonly LogService _logService;

    public UpdaterLaunchService(LogService logService)
    {
        _logService = logService;
    }

    public string ResolveUpdaterPath()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "PlusPlusPen.Updater.exe");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Güncelleştirme Merkezi bulunamadı.", path);
        }

        return path;
    }

    public void Launch(
        string packagePath,
        string installDir,
        string appExePath,
        int mainPid,
        string currentVersion,
        string? expectedSha256)
    {
        var updaterPath = ResolveUpdaterPath();
        var args = new StringBuilder();
        AppendArgument(args, "--package", packagePath);
        AppendArgument(args, "--install-dir", installDir);
        AppendArgument(args, "--app-exe", appExePath);
        AppendArgument(args, "--main-pid", mainPid.ToString());
        AppendArgument(args, "--current-version", currentVersion);

        if (!string.IsNullOrWhiteSpace(expectedSha256))
        {
            AppendArgument(args, "--expected-sha256", expectedSha256);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = updaterPath,
            WorkingDirectory = Path.GetDirectoryName(updaterPath) ?? AppContext.BaseDirectory,
            Arguments = args.ToString(),
            UseShellExecute = true
        };

        Process.Start(startInfo);
        _logService.LogInfo($"Güncelleştirme Merkezi başlatıldı: {packagePath}");
    }

    private static void AppendArgument(StringBuilder builder, string name, string value)
    {
        if (builder.Length > 0)
        {
            builder.Append(' ');
        }

        builder.Append(name)
            .Append(' ')
            .Append('"')
            .Append(value.Replace("\"", "\\\""))
            .Append('"');
    }
}
