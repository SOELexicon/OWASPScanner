using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityScanner.Infrastructure.Configuration;

namespace SecurityScanner.Infrastructure.Logging;

public static class LoggingExtensions
{
    public static IServiceCollection AddSecurityScannerLogging(
        this IServiceCollection services,
        bool jsonOutputMode = false)
    {
        services.AddLogging(builder =>
        {
            if (!jsonOutputMode)
            {
                builder.AddConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                });
            }

            builder.AddProvider(new FileLoggerProvider());
        });

        services.AddSingleton<IFileLogger, FileLogger>();
        
        return services;
    }
}

public interface IFileLogger
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
    void LogDebug(string message, params object[] args);
}

public class FileLogger : IFileLogger
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public FileLogger(IOptions<LoggingSettings> settings)
    {
        _logDirectory = settings.Value.LogDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public void LogInformation(string message, params object[] args)
    {
        WriteLog("INFO", message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        WriteLog("WARN", message, args);
    }

    public void LogError(string message, params object[] args)
    {
        WriteLog("ERROR", message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        var fullMessage = $"{string.Format(message, args)}\nException: {exception}";
        WriteLog("ERROR", fullMessage);
    }

    public void LogDebug(string message, params object[] args)
    {
        WriteLog("DEBUG", message, args);
    }

    private void WriteLog(string level, string message, params object[] args)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                var logEntry = $"[{timestamp}] [{level}] {formattedMessage}{Environment.NewLine}";
                
                var logFileName = $"scanner-{DateTime.UtcNow:yyyy-MM-dd}.log";
                var logFilePath = Path.Combine(_logDirectory, logFileName);
                
                File.AppendAllText(logFilePath, logEntry);
            }
            catch
            {
                // Silently fail to avoid logging loops
            }
        }
    }
}

public class FileLoggerProvider : ILoggerProvider
{
    private readonly Dictionary<string, FileLoggerWrapper> _loggers = new();
    private readonly object _lock = new();
    private bool _disposed = false;

    public ILogger CreateLogger(string categoryName)
    {
        lock (_lock)
        {
            if (!_loggers.TryGetValue(categoryName, out var logger))
            {
                logger = new FileLoggerWrapper(categoryName);
                _loggers[categoryName] = logger;
            }
            return logger;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                foreach (var logger in _loggers.Values)
                {
                    logger.Dispose();
                }
                _loggers.Clear();
            }
            _disposed = true;
        }
    }
}

public class FileLoggerWrapper : ILogger, IDisposable
{
    private readonly string _categoryName;
    private readonly string _logDirectory = "logs";

    public FileLoggerWrapper(string categoryName)
    {
        _categoryName = categoryName;
        Directory.CreateDirectory(_logDirectory);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var level = logLevel.ToString().ToUpper();
        
        var logEntry = $"[{timestamp}] [{level}] [{_categoryName}] {message}";
        
        if (exception != null)
        {
            logEntry += $"{Environment.NewLine}Exception: {exception}";
        }
        
        logEntry += Environment.NewLine;

        try
        {
            var logFileName = $"scanner-{DateTime.UtcNow:yyyy-MM-dd}.log";
            var logFilePath = Path.Combine(_logDirectory, logFileName);
            File.AppendAllText(logFilePath, logEntry);
        }
        catch
        {
            // Silently fail to avoid logging loops
        }
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}