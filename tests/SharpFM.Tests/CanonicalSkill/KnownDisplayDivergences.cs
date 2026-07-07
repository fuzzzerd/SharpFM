namespace SharpFM.Tests.CanonicalSkill;

/// <summary>
/// Fixtures whose steps do NOT survive display-text mutation today (their
/// display grammar or FromDisplay parser is lossy) even though the step
/// claims <c>IsFullyEditable</c>. This is the display-accuracy worklist: as
/// each step's display pair becomes faithful — or the step is sealed instead —
/// remove its fixtures here; the round-trip test's guard fails until you do,
/// so the list cannot silently drift out of sync with reality.
/// </summary>
public static class KnownDisplayDivergences
{
    // Empty: every documented step either survives display-text mutation or
    // seals itself (IsFullyEditable false) when it carries state the display
    // grammar cannot express — the editor anchor-preserves those instances.
    // Add a fixture name here only if a future skill refresh introduces a
    // configuration the display pair does not yet handle.
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
    };
}
