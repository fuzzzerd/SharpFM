using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RelookupFieldContentsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 40;
    public const string XmlName = "Relookup Field Contents";

    public bool WithDialog { get; set; }
    public FieldRef Target { get; set; }

    public RelookupFieldContentsStep(
        bool withDialog = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(null, enabled)
    {
        WithDialog = withDialog;
        Target = target ?? FieldRef.ForField("", 0, "");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "True" : "False")),
            Target.ToXml("Field"));

    public override string ToDisplayLine() =>
        "Relookup Field Contents [ " + "With dialog: " + (WithDialog ? "On" : "Off") + " ; " + Target.ToDisplayString() + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog_v = step.Element("NoInteract")?.Attribute("state")?.Value == "True";
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, "");
        return new RelookupFieldContentsStep(withDialog_v, target, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool withDialog_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        FieldRef target = FieldRef.ForField("", 0, "");
        foreach (var tok in tokens) { if (!tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tok)) { target = FieldRef.FromDisplayToken(tok); break; } }
        return new RelookupFieldContentsStep(withDialog_v, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/relookup-field-contents.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "NoInteract",
                XmlElement = "NoInteract",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "With dialog",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
            },
            new ParamMetadata
            {
                Name = "Field",
                XmlElement = "Field",
                Type = "field",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
