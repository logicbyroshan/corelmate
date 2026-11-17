using System;
using System.IO;

namespace CorelMate.Infrastructure;

public interface ICorelMateLogger
{
    void Info(string message);
    void Error(string message, Exception? exception);
}

public sealed class NullCorelMateLogger : ICorelMateLogger
{
    public void Info(string message) { }
    public void Error(string message, Exception? exception) { }
}

public sealed class FileCorelMateLogger : ICorelMateLogger
{
    private static readonly object SyncLock = new object();
    private readonly string logFilePath;

    public FileCorelMateLogger(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            logFilePath = customPath!;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logDir = Path.Combine(appData, "CorelMate", "Logs");
            Directory.CreateDirectory(logDir);
            logFilePath = Path.Combine(logDir, "corelmate.log");
        }
    }

    public string LogFilePath => logFilePath;

    public void Info(string message)
    {
        Write("INFO", message, null);
    }

    public void Error(string message, Exception? exception)
    {
        Write("ERROR", message, exception);
    }

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var line = $"[{timestamp}] [{level}] {message}";
            if (exception != null)
            {
                line += $"{Environment.NewLine}{exception.GetType().FullName}: {exception.Message}{Environment.NewLine}{exception.StackTrace}";
            }

            lock (SyncLock)
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Silently ignore logging failures to avoid impacting host operations
        }
    }
}
