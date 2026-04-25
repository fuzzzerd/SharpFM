using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Logging;
using NLog;

namespace SharpFM;

/// <summary>
/// Avalonia Log Sink that writes to NLog Loggers.
///
/// <para>Hot path note: Avalonia's framework code emits high-volume
/// Verbose/Debug/Info events during layout, input, and rendering. The
/// app's <c>nlog.config</c> blackholes <c>Avalonia*</c> events at those
/// levels via a <c>final="true"</c> rule, so they have nowhere to go —
/// but if <see cref="IsEnabled"/> says <c>true</c>, Avalonia still
/// formats arguments and calls <see cref="Log"/>, which then walks NLog's
/// rule engine before discarding. Profiling on a small script during
/// typing showed this sink consuming ~318ms of inclusive CPU over a 30s
/// trace — more than any other SharpFM-attributed path.</para>
///
/// <para>Two-part fix: gate <see cref="IsEnabled"/> at Warning so Avalonia
/// short-circuits before the formatting work, and cache the per-source
/// <see cref="ILogger"/> so the surviving events skip
/// <see cref="LogManager.GetLogger(string)"/> on every call.</para>
/// </summary>
[ExcludeFromCodeCoverage]
public class AvaloniaNLogSink : ILogSink
{
    private static readonly ILogger DefaultLogger =
        LogManager.GetLogger(typeof(AvaloniaNLogSink).ToString());

    private static readonly ConcurrentDictionary<Type, ILogger> LoggerCache = new();

    /// <summary>
    /// Gate at Warning to match the intent of <c>nlog.config</c>'s
    /// <c>Avalonia*</c> blackhole rule. Avalonia framework code routinely
    /// emits hundreds of Verbose/Debug/Info events per second during
    /// typing — none of which would reach a target anyway.
    /// </summary>
    public bool IsEnabled(LogEventLevel level, string area) =>
        level >= LogEventLevel.Warning;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Log(level, area, source, messageTemplate, Array.Empty<object?>());
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        ILogger logger = source is null
            ? DefaultLogger
            : LoggerCache.GetOrAdd(source.GetType(), static t => LogManager.GetLogger(t.ToString()));

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

[ExcludeFromCodeCoverage]
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