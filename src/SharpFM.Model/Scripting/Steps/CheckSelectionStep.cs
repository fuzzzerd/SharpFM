using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CheckSelectionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 18;
    public const string XmlName = "Check Selection";

    public bool Select { get; set; }
    public FieldRef Target { get; set; }

    public CheckSelectionStep(
        bool select = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(null, enabled)
    {
        Select = select;
        Target = target ?? FieldRef.ForField("", 0, "");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("SelectAll", new XAttribute("state", Select ? "True" : "False")),
            Target.ToXml("Field"));

    public override string ToDisplayLine() =>
        "Check Selection [ " + "Select: " + (Select ? "On" : "Off") + " ; " + Target.ToDisplayString() + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var select_v = step.Element("SelectAll")?.Attribute("state")?.Value == "True";
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, "");
        return new CheckSelectionStep(select_v, target, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool select_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Select:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); select_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        FieldRef target = FieldRef.ForField("", 0, "");
        foreach (var tok in tokens) { if (!tok.StartsWith("Select:", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tok)) { target = FieldRef.FromDisplayToken(tok); break; } }
        return new CheckSelectionStep(select_v, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "spelling",
        HelpUrl = "https://help.claris.com/en/pro-help/content/check-selection.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "SelectAll",
                XmlElement = "SelectAll",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Select",
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
