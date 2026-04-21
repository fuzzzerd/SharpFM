namespace SharpFM.Model.Scripting.Registry;

/// <summary>
/// Platform-specific behavioural differences for a script step. Each
/// property holds a prose sentence describing how the step behaves on
/// that runtime, populated only where it differs from the standard
/// FileMaker Pro client.
/// </summary>
public sealed record StepPlatformNotes
{
    public string? Server { get; init; }
    public string? WebDirect { get; init; }
    public string? Go { get; init; }
}
