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
        // Save a Copy as XML — the configured forms emit a different element set
        // (OutputEntireBinaryData / SpecifyJSONOptions / SaXML) than the POCO models.
        "003-SaveACopyAsXML-1",
        "003-SaveACopyAsXML-2",

        // Import/Export Records — DataSourceType / source descriptors and lists of
        // complex value types not yet modelled.
        "035-ImportRecords",
        "036-ExportRecords",

        // Multi-variant dialog: Title/Message and the dimension calcs are each
        // independently optional, and the <Buttons> block is a button list.
        "087-ShowCustomDialog-1",
        "087-ShowCustomDialog-2",
        "087-ShowCustomDialog-3",

        // Save Records as PDF — the deep <PDFOptions> structure (Document/Pages/
        // Security/View with a leading PDFSaveType) is not fully modelled.
        "144-SaveRecordsAsPDF-1",
        "144-SaveRecordsAsPDF-2",
    };
}
