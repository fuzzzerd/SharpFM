namespace SharpFM.Model.Scripting.Registry;

/// <summary>
/// Behavioural intelligence for a script step — the "what you should
/// know" content agents and tooltip UIs draw from. Mirrors the
/// structure of upstream agentic-fm's catalog <c>notes</c> object.
/// All fields are nullable; populate only when a POCO has real content
/// to contribute.
/// </summary>
public sealed record StepNotes
{
    /// <summary>General usage and side-effect behaviour.</summary>
    public string? Behavioral { get; init; }

    /// <summary>Hard rules FileMaker enforces or silently breaks.</summary>
    public string? Constraints { get; init; }

    /// <summary>Subtle behaviours that cause real-world bugs.</summary>
    public string? Gotchas { get; init; }

    /// <summary>Performance guidance.</summary>
    public string? Performance { get; init; }

    /// <summary>Per-runtime divergence from the standard FileMaker Pro client.</summary>
    public StepPlatformNotes? Platform { get; init; }
}
