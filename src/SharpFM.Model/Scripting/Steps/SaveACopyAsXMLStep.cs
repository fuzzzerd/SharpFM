using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
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
public sealed class SaveACopyAsXMLStep : ScriptStep, IStepFactory
{
    public const int XmlId = 3;
    public const string XmlName = "Save a Copy as XML";

    public bool IncludeDetailsForAnalysisTools { get; set; }
    public bool? OutputEntireBinaryData { get; set; }
    public bool? SpecifyJSONOptions { get; set; }
    public Calculation? JsonOptions { get; set; }

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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Save a Copy as XML [ Include details for analysis tools: "
        + (IncludeDetailsForAnalysisTools ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SaveACopyAsXMLStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var include = hrParams
            .Select(h => h.Trim())
            .Where(t => t.StartsWith("Include details for analysis tools:", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Substring(35).Trim().Equals("On", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        return new SaveACopyAsXMLStep(include, enabled: enabled);
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
            new BoolStateChild("OutputEntireBinaryData") { Optional = true },
            new BoolStateChild("SpecifyJSONOptions") { Optional = true },
            // <SaXML> is emitted only when the JSON export options are set.
            new WrapperChild("SaXML",
            [
                new WrapperChild("JSONOptions",
                [
                    new BareCalcChild { PocoProperty = "JsonOptions" },
                ]),
            ]) { PocoProperty = "JsonOptions", Optional = true },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Option",
                XmlElement = "Option",
                Type = "flagBoolean",
                XmlAttr = "state",
                HrLabel = "Include details for analysis tools",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
