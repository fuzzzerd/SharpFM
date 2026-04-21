using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert Text carries a literal <c>&lt;Text&gt;...&lt;/Text&gt;</c>
/// (not a Calculation/CDATA block) and a dual-format Field target. The
/// Text element is required.
/// </summary>
public sealed class InsertTextStep : ScriptStep, IStepFactory
{
    public const int XmlId = 61;
    public const string XmlName = "Insert Text";

    public bool SelectAll { get; set; }
    public FieldRef? Target { get; set; }
    public string Text { get; set; }

    public InsertTextStep(bool selectAll = true, FieldRef? target = null, string text = "", bool enabled = true)
        : base(null, enabled)
    {
        SelectAll = selectAll;
        Target = target;
        Text = text;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("SelectAll", new XAttribute("state", SelectAll ? "True" : "False")),
            new XElement("Text", Text));
        if (Target is not null) step.Add(Target.ToXml("Field"));
        return step;
    }

    public override string ToDisplayLine()
    {
        var selectPart = SelectAll ? "Select ; " : "";
        var targetPart = Target is null ? "" : $"Target: {Target.ToDisplayString()} ; ";
        return $"Insert Text [ {selectPart}{targetPart}\"{Text}\" ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var selectAll = step.Element("SelectAll")?.Attribute("state")?.Value == "True";
        var text = step.Element("Text")?.Value ?? "";
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new InsertTextStep(selectAll, target, text, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool selectAll = false;
        FieldRef? target = null;
        string text = "";
        bool textSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Select", StringComparison.OrdinalIgnoreCase))
                selectAll = true;
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            else if (!textSeen && !string.IsNullOrWhiteSpace(t))
            {
                text = t;
                if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length >= 2)
                    text = text.Substring(1, text.Length - 2);
                textSeen = true;
            }
        }
        return new InsertTextStep(selectAll, target, text, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-text.html",
        Params =
        [
            new ParamMetadata { Name = "SelectAll", XmlElement = "SelectAll", XmlAttr = "state", Type = "boolean", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "fieldOrVariable", HrLabel = "Target" },
            new ParamMetadata { Name = "Text", XmlElement = "Text", Type = "text", Required = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
