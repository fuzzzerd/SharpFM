using System;
using Avalonia;
using Avalonia.Logging;
using NLog;

namespace SharpFM.App;

/// <summary>
/// Avalonia Log Sink that writes to NLog Loggers.
/// </summary>
public class AvaloniaNLogSink : ILogSink
{
    /// <summary>
    /// AvaloniaNLogSink is always enabled.
    /// </summary>
    public bool IsEnabled(LogEventLevel level, string area) => true;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Log(level, area, source, messageTemplate, Array.Empty<object?>());
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        ILogger? logger = source is not null ? LogManager.GetLogger(source.GetType().ToString())
            : LogManager.GetLogger(typeof(AvaloniaNLogSink).ToString());

        logger.Log(LogLevelToNLogLevel(level), $"{area}: {messageTemplate}", propertyValues);
    }

    private static LogLevel LogLevelToNLogLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Info,
            LogEventLevel.Warning => LogLevel.Warn,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Fatal,
            _ => LogLevel.Trace,
        };
    }
}

public static class NLogSinkExtensions
{
    /// <summary>
    /// Creates an instance of the AvaloniaNLogSink and assigns it to the global sink.
    /// </summary>
    public static AppBuilder LogToNLog(
        this AppBuilder builder)
    {
        Avalonia.Logging.Logger.Sink = new AvaloniaNLogSink();
        return builder;
    }
}