using System.Diagnostics;

namespace DolCon.Core.Services;

/// <summary>
/// Log levels for filtering output.
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

/// <summary>
/// Simple logging service with configurable log levels.
/// Logs are written to Debug output and optionally to a file.
/// </summary>
public static class LogService
{
    private static LogLevel _minimumLevel = LogLevel.Warning;
    private static readonly object _lock = new();
    private static string? _logFilePath;
    private static bool _fileLoggingEnabled;

    /// <summary>
    /// Gets or sets the minimum log level. Messages below this level are filtered out.
    /// Default is Warning (Debug and Info are filtered by default).
    /// </summary>
    public static LogLevel MinimumLevel
    {
        get => _minimumLevel;
        set => _minimumLevel = value;
    }

    /// <summary>
    /// Enable verbose logging (Debug and Info levels included).
    /// </summary>
    public static void EnableVerbose()
    {
        _minimumLevel = LogLevel.Debug;
        Info("Verbose logging enabled");
    }

    /// <summary>
    /// Enable file logging to the specified path.
    /// </summary>
    public static void EnableFileLogging(string path)
    {
        _logFilePath = path;
        _fileLoggingEnabled = true;
        Info($"File logging enabled: {path}");
    }

    /// <summary>
    /// Disable file logging.
    /// </summary>
    public static void DisableFileLogging()
    {
        _fileLoggingEnabled = false;
        _logFilePath = null;
    }

    /// <summary>
    /// Log a debug message (filtered by default).
    /// </summary>
    public static void Debug(string message, string? category = null)
    {
        Log(LogLevel.Debug, message, category);
    }

    /// <summary>
    /// Log an info message (filtered by default).
    /// </summary>
    public static void Info(string message, string? category = null)
    {
        Log(LogLevel.Info, message, category);
    }

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public static void Warning(string message, string? category = null)
    {
        Log(LogLevel.Warning, message, category);
    }

    /// <summary>
    /// Log an error message.
    /// </summary>
    public static void Error(string message, string? category = null)
    {
        Log(LogLevel.Error, message, category);
    }

    /// <summary>
    /// Log an exception with its stack trace.
    /// </summary>
    public static void Exception(Exception ex, string? context = null)
    {
        var message = context != null
            ? $"{context}: {ex.Message}\n{ex.StackTrace}"
            : $"{ex.Message}\n{ex.StackTrace}";
        Log(LogLevel.Error, message, "Exception");
    }

    private static void Log(LogLevel level, string message, string? category)
    {
        if (level < _minimumLevel) return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpperInvariant().PadRight(7);
        var categoryStr = category != null ? $"[{category}] " : "";
        var formattedMessage = $"[{timestamp}] {levelStr} {categoryStr}{message}";

        // Always write to Debug output
        System.Diagnostics.Debug.WriteLine(formattedMessage);

        // Write to file if enabled
        if (_fileLoggingEnabled && _logFilePath != null)
        {
            WriteToFile(formattedMessage);
        }
    }

    private static void WriteToFile(string message)
    {
        if (_logFilePath == null) return;

        try
        {
            lock (_lock)
            {
                File.AppendAllText(_logFilePath, message + Environment.NewLine);
            }
        }
        catch
        {
            // Silently fail if file logging fails - don't crash the app
        }
    }
}
