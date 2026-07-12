using System;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Save a Copy as XML (3). Canonical form (skill, windows/files reference):
/// <c>&lt;Option&gt;</c> ("include details for analysis tools") always, then the
/// FM 26 options — optional <c>&lt;OutputEntireBinaryData&gt;</c> and
/// <c>&lt;SpecifyJSONOptions&gt;</c> flags and an optional
/// <c>&lt;SaXML&gt;&lt;JSONOptions&gt;&lt;Calculation&gt;…</c> block carrying the
/// catalog/JSON export options. The pre-FM 26 form is just <c>&lt;Option&gt;</c>.
/// The FM 26 flags are nullable so an absent flag stays distinct from "present
/// and False".
/// </summary>
public sealed class SaveACopyAsXMLStep : ScriptStep<SaveACopyAsXMLStep>, IStepFactory
{
    public const int XmlId = 3;
    public const string XmlName = "Save a Copy as XML";

    public bool IncludeDetailsForAnalysisTools { get; set; }
    public bool? OutputEntireBinaryData { get; set; }
    public bool? SpecifyJSONOptions { get; set; }
    public Calculation? JsonOptions { get; set; }

    /// <summary>
    /// Display edits are anchor-preserved when any FM 26 option is present —
    /// the display line carries only the analysis-tools flag, never the
    /// binary-data/JSON-options state.
    /// </summary>
    public override bool IsFullyEditable =>
        OutputEntireBinaryData is null && SpecifyJSONOptions is null && JsonOptions is null;

    private SaveACopyAsXMLStep() : base(false) { }

    public SaveACopyAsXMLStep(
        bool includeDetailsForAnalysisTools = false,
        bool? outputEntireBinaryData = null,
        bool? specifyJSONOptions = null,
        Calculation? jsonOptions = null,
        bool enabled = true)
        : base(enabled)
    {
        IncludeDetailsForAnalysisTools = includeDetailsForAnalysisTools;
        OutputEntireBinaryData = outputEntireBinaryData;
        SpecifyJSONOptions = specifyJSONOptions;
        JsonOptions = jsonOptions;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-a-copy-as-xml.html",
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "IncludeDetailsForAnalysisTools", HrLabel = "Include details for analysis tools" },
            // FM 26 flags are bool? — absent stays distinct from "present and False".
            new BoolStateChild("OutputEntireBinaryData") { Optional = true, Display = DisplayMode.Hidden },
            new BoolStateChild("SpecifyJSONOptions") { Optional = true, Display = DisplayMode.Hidden },
            // <SaXML> is emitted only when the JSON export options are set.
            new WrapperChild("SaXML",
            [
                new WrapperChild("JSONOptions",
                [
                    new BareCalcChild { PocoProperty = "JsonOptions", Display = DisplayMode.Hidden },
                ]),
            ]) { PocoProperty = "JsonOptions", Optional = true },
        ],
    };
}
