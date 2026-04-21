using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GoToFieldStep : ScriptStep, IStepFactory
{
    public const int XmlId = 17;
    public const string XmlName = "Go to Field";

    public bool SelectPerform { get; set; }
    public FieldRef Target { get; set; }

    public GoToFieldStep(
        bool selectPerform = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        SelectPerform = selectPerform;
        Target = target ?? FieldRef.ForField("", 0, "");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("SelectAll", new XAttribute("state", SelectPerform ? "True" : "False")),
            Target.ToXml("Field"));

    public override string ToDisplayLine() =>
        "Go to Field [ " + "Select/perform: " + (SelectPerform ? "On" : "Off") + " ; " + Target.ToDisplayString() + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var selectPerform_v = step.Element("SelectAll")?.Attribute("state")?.Value == "True";
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, "");
        return new GoToFieldStep(selectPerform_v, target, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool selectPerform_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Select/perform:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(15).Trim(); selectPerform_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        FieldRef target = FieldRef.ForField("", 0, "");
        foreach (var tok in tokens) { if (!tok.StartsWith("Select/perform:", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tok)) { target = FieldRef.FromDisplayToken(tok); break; } }
        return new GoToFieldStep(selectPerform_v, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-field.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "SelectAll",
                XmlElement = "SelectAll",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Select/perform",
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
