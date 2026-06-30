namespace SharpFM.Tests.CanonicalSkill;

/// <summary>
/// Fixtures whose current POCO output diverges from the skill's canonical
/// form. This is the divergence inventory surfaced in phase 2 — the baseline
/// the audit shrinks. As each step is migrated to the shape-driven renderer
/// and starts round-tripping, remove its fixture name here; the round-trip
/// test's guard fails until you do, so the list cannot silently drift out of
/// sync with reality.
///
/// <para>
/// The remaining entries are the structurally hard cases that need further
/// engine work or preserve-don't-synthesize handling — grouped below with the
/// specific capability each is waiting on.
/// </para>
/// </summary>
public static class KnownDivergences
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        // Attribute-dictionaries + lists of complex value types (ImportOptions,
        // Profile, ExportEntries, TargetFields) with no shape primitive yet.
        "003-SaveACopyAsXML-1",
        "003-SaveACopyAsXML-2",
        "035-ImportRecords",
        "036-ExportRecords",

        // Multi-variant dialog: Title/Message and the dimension calcs are each
        // independently optional across variants.
        "087-ShowCustomDialog-1",
        "087-ShowCustomDialog-2",
        "087-ShowCustomDialog-3",

        // SerialNumberOptions value type omits increment/InitialValue attributes
        // the canonical <SerialNumbers/> carries.
        "091-ReplaceFieldContents",

        // Populated wrappers (<Profile>, <PDFOptions>) that must be omitted when
        // unconfigured but carry attributes/children when set — needs an
        // optional value-type wrapper primitive.
        "143-SaveRecordsAsExcel",
        "144-SaveRecordsAsPDF-1",
        "144-SaveRecordsAsPDF-2",

        // <URL custom="False"><Calculation>… — an element carrying both an
        // attribute and a calc child; no primitive models that.
        "146-SetWebViewer-1",
        "146-SetWebViewer-2",
        "146-SetWebViewer-3",

        // PerformScriptTarget VariantBlock — StepXmlParser throws NotSupported on
        // VariantBlock parsing.
        "210-PerformScriptOnServerWithCallback",
    };
}
