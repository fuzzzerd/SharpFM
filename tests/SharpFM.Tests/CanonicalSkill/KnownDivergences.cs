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
/// Entries are fixture base names (file name without extension). A trailing
/// <c>-N</c> marks one of several documented variants for the same step id
/// (e.g. configured vs unconfigured forms).
/// </para>
/// </summary>
public static class KnownDivergences
{
    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        // Complex / multi-variant save forms (optional <UniversalPathList>, JSON options).
        "003-SaveACopyAsXML-1",
        "003-SaveACopyAsXML-2",
        "035-ImportRecords",
        "036-ExportRecords",
        "037-SaveACopyAs",
        // Value types that over-emit empty attributes (SendEventTarget, SpeechOptions).
        "057-SendEvent",
        "066-Speak",
        // Conditional variable-<Text> marker before <Field>.
        "077-InsertCalculatedResult",
        "192-WriteToDataFile",
        "193-ReadFromDataFile",
        "203-ExecuteFileMakerDataAPI",
        // Multi-variant optional dimensions / title.
        "087-ShowCustomDialog-1",
        "087-ShowCustomDialog-2",
        "087-ShowCustomDialog-3",
        // Comment CR/LF normalization.
        "089-Comment-1",
        // Element reorders / renames.
        "091-ReplaceFieldContents",
        "097-SetZoomLevel-2",
        "119-MoveResizeWindow",
        "121-CloseWindow-1",
        "123-SelectWindow",
        "124-SetWindowTitle",
        "139-ConvertFile",
        "146-SetWebViewer-1",
        "146-SetWebViewer-2",
        "146-SetWebViewer-3",
        "195-SetDataFilePosition",
        "205-OpenTransaction",
        "207-RevertTransaction",
        "210-PerformScriptOnServerWithCallback",
        "228-GoToListOfRecords",
        // Conditionally-omitted populated wrappers (<Profile>, <PDFOptions>).
        "143-SaveRecordsAsExcel",
        "144-SaveRecordsAsPDF-1",
        "144-SaveRecordsAsPDF-2",
        // Verbatim device-options subtree (needs Passthrough parsing).
        "161-InsertFromDevice",
        // PerformJavaScript optional FunctionName.
        "175-PerformJavaScriptInWebViewer-1",
        // Configure Prompt Template wrapper.
        "226-ConfigurePromptTemplate",
    };
}
