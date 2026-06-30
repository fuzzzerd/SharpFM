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
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        // Save a Copy as XML — the canonical configured form emits an entirely
        // different element set (OutputEntireBinaryData / SpecifyJSONOptions /
        // SaXML) than the generic path/calc the POCO currently models; it needs
        // a dedicated remodel of its XML/JSON export options.
        "003-SaveACopyAsXML-1",
        "003-SaveACopyAsXML-2",
    };
}
