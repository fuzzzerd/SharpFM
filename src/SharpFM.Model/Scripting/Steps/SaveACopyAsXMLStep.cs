using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
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

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", IncludeDetailsForAnalysisTools ? "True" : "False")));
        if (OutputEntireBinaryData is { } oebd)
            step.Add(new XElement("OutputEntireBinaryData", new XAttribute("state", oebd ? "True" : "False")));
        if (SpecifyJSONOptions is { } sjo)
            step.Add(new XElement("SpecifyJSONOptions", new XAttribute("state", sjo ? "True" : "False")));
        if (JsonOptions is not null)
            step.Add(new XElement("SaXML",
                new XElement("JSONOptions", JsonOptions.ToXml("Calculation"))));
        return step;
    }

    public override string ToDisplayLine() =>
        "Save a Copy as XML [ Include details for analysis tools: "
        + (IncludeDetailsForAnalysisTools ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var include = step.Element("Option")?.Attribute("state")?.Value == "True";
        bool? oebd = step.Element("OutputEntireBinaryData") is { } o ? o.Attribute("state")?.Value == "True" : null;
        bool? sjo = step.Element("SpecifyJSONOptions") is { } s ? s.Attribute("state")?.Value == "True" : null;
        var jsonCalc = step.Element("SaXML")?.Element("JSONOptions")?.Element("Calculation");
        var json = jsonCalc is not null ? Calculation.FromXml(jsonCalc) : null;
        return new SaveACopyAsXMLStep(include, oebd, sjo, json, enabled);
    }

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
