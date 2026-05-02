using System.IO;
using System.Text;

namespace PlusPlusPen.Updater.Services;

public sealed class UpdaterLogService
{
    private readonly string _logPath;

    public UpdaterLogService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PlusPlusPen",
            "logs");
        Directory.CreateDirectory(folder);
        _logPath = Path.Combine(folder, "updater.log");
    }

    public string LogPath => _logPath;

    public void Info(string message)
    {
        Write("INFO", message, null);
    }

    public void Error(string message, Exception exception)
    {
        Write("ERROR", message, exception);
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
