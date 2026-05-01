using System.Diagnostics;
using System.IO;
using System.Text;

namespace PlusPlusPen.Services;

public sealed class LogService
{
    private readonly string _logFolder;
    private readonly string _logPath;

    public LogService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PlusPlusPen");
        _logFolder = Path.Combine(appDataPath, "logs");
        Directory.CreateDirectory(_logFolder);
        _logPath = Path.Combine(_logFolder, "pluspluspen.log");
    }

    public string LogPath => _logPath;

    public void LogInfo(string message)
    {
        Write("INFO", message, null);
    }

    public void LogError(string message, Exception? exception)
    {
        Write("ERROR", message, exception);
    }

    public void Clear()
    {
        File.WriteAllText(_logPath, string.Empty, Encoding.UTF8);
    }

    public void OpenLog()
    {
        try
        {
            if (!File.Exists(_logPath))
            {
                File.WriteAllText(_logPath, string.Empty, Encoding.UTF8);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _logPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LogError("Log dosyasi acilamadi.", ex);
        }
    }

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            var builder = new StringBuilder();
            builder.Append('[')
                .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Append("] ")
                .Append(level)
                .Append(" - ")
                .Append(message);

            if (exception is not null)
            {
                builder.AppendLine()
                    .Append(exception);
            }

            builder.AppendLine();
            File.AppendAllText(_logPath, builder.ToString(), Encoding.UTF8);
        }
        catch
        {
        }
    }
}
