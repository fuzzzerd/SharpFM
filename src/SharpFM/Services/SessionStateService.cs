using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SharpFM.Models;

namespace SharpFM.Services;

/// <summary>
/// Persists the UI session (open tabs + active tab) as a single JSON file at
/// <c>%LocalAppData%/SharpFM/session.json</c>. Read on launch, written on
/// window close. Malformed or missing files yield <see cref="SessionState.Empty"/>;
/// write failures are logged and swallowed so a failing disk can't crash app exit.
/// </summary>
public class SessionStateService
{
    private readonly ILogger _logger;

    public string FilePath { get; }

    public SessionStateService(ILogger logger)
        : this(logger, DefaultPath())
    {
    }

    public SessionStateService(ILogger logger, string filePath)
    {
        _logger = logger;
        FilePath = filePath;
    }

    private static string DefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SharpFM", "session.json");

    public SessionState Load()
    {
        if (!File.Exists(FilePath)) return SessionState.Empty;

        try
        {
            var text = File.ReadAllText(FilePath);
            var state = JsonSerializer.Deserialize<SessionState>(text);
            if (state is null) return SessionState.Empty;
            // System.Text.Json doesn't enforce non-nullable record properties,
            // so a hand-edited or partial file can produce a SessionState with
            // a null OpenTabs list. Coerce here so the restore path can trust
            // its iteration target.
            return state.OpenTabs is null
                ? state with { OpenTabs = [] }
                : state;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read session state from {Path}; ignoring.", FilePath);
            return SessionState.Empty;
        }
    }

    public void Save(SessionState state)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var text = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write session state to {Path}.", FilePath);
        }
    }
}
