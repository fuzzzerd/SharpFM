namespace SharpFM.Tests.CanonicalSkill;

/// <summary>
/// Fixtures whose current POCO output diverges from the skill's canonical
/// form. This is the divergence inventory surfaced in phase 2 — the baseline
/// the audit shrinks. As each step is migrated to the shape-driven renderer
/// and starts round-tripping, remove its fixture name here; the round-trip
/// test's guard fails until you do, so the list cannot silently drift out of
/// sync with reality.
/// </summary>
public static class KnownDivergences
{
    // Empty: every documented step now round-trips to the skill's canonical form.
    // Add a fixture name here only if a future skill refresh introduces a step
    // whose canonical shape the model does not yet reproduce.
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal);
}
