using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PasteStep : ScriptStep, IStepFactory
{
    public const int XmlId = 48;
    public const string XmlName = "Paste";

    public bool Select { get; set; }
    public bool NoStyle { get; set; }
    public bool LinkIfAvailable { get; set; }
    public FieldRef Target { get; set; }

    public PasteStep(
        bool select = false,
        bool noStyle = false,
        bool linkIfAvailable = false,
        FieldRef? target = null,
        bool enabled = true)
        : base(null, enabled)
    {
        Select = select;
        NoStyle = noStyle;
        LinkIfAvailable = linkIfAvailable;
        Target = target ?? FieldRef.ForField("", 0, "");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("SelectAll", new XAttribute("state", Select ? "True" : "False")),
            new XElement("NoStyle", new XAttribute("state", NoStyle ? "True" : "False")),
            new XElement("LinkAvail", new XAttribute("state", LinkIfAvailable ? "True" : "False")),
            Target.ToXml("Field"));

    public override string ToDisplayLine() =>
        "Paste [ " + "Select: " + (Select ? "On" : "Off") + " ; " + "No style: " + (NoStyle ? "On" : "Off") + " ; " + "Link if available: " + (LinkIfAvailable ? "On" : "Off") + " ; " + "Table::Field: " + Target.ToDisplayString() + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var select_v = step.Element("SelectAll")?.Attribute("state")?.Value == "True";
        var noStyle_v = step.Element("NoStyle")?.Attribute("state")?.Value == "True";
        var linkIfAvailable_v = step.Element("LinkAvail")?.Attribute("state")?.Value == "True";
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, "");
        return new PasteStep(select_v, noStyle_v, linkIfAvailable_v, target, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool select_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Select:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); select_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool noStyle_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("No style:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(9).Trim(); noStyle_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool linkIfAvailable_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Link if available:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(18).Trim(); linkIfAvailable_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        FieldRef target = FieldRef.ForField("", 0, "");
        foreach (var tok in tokens) { if (tok.StartsWith("Table::Field:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); target = FieldRef.FromDisplayToken(v); break; } }
        return new PasteStep(select_v, noStyle_v, linkIfAvailable_v, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "editing",
        HelpUrl = "https://help.claris.com/en/pro-help/content/paste.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "SelectAll",
                XmlElement = "SelectAll",
                Type = "flagBoolean",
                XmlAttr = "state",
                HrLabel = "Select",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "NoStyle",
                XmlElement = "NoStyle",
                Type = "flagBoolean",
                XmlAttr = "state",
                HrLabel = "No style",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "LinkAvail",
                XmlElement = "LinkAvail",
                Type = "flagBoolean",
                XmlAttr = "state",
                HrLabel = "Link if available",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "Field",
                XmlElement = "Field",
                Type = "field",
                HrLabel = "Table::Field",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
