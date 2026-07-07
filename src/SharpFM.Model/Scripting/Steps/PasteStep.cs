using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PasteStep : ScriptStep, IStepFactory
{
    public const int XmlId = 48;
    public const string XmlName = "Paste";

    public bool Select { get; set; }
    public bool NoStyle { get; set; }
    public bool LinkIfAvailable { get; set; }
    public FieldRef? Target { get; set; }

    private PasteStep() : base(false) { }

    public PasteStep(
        bool select = false,
        bool noStyle = false,
        bool linkIfAvailable = false,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        Select = select;
        NoStyle = noStyle;
        LinkIfAvailable = linkIfAvailable;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Paste [ " + "Select: " + (Select ? "On" : "Off") + " ; " + "No style: " + (NoStyle ? "On" : "Off") + " ; " + "Link if available: " + (LinkIfAvailable ? "On" : "Off") + " ; " + "Table::Field: " + (Target?.ToDisplayString() ?? "") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PasteStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool select_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Select:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); select_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool noStyle_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("No style:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(9).Trim(); noStyle_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool linkIfAvailable_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Link if available:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(18).Trim(); linkIfAvailable_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        FieldRef? target = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Table::Field:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); if (v.Length > 0) target = FieldRef.FromDisplayToken(v); break; } }
        return new PasteStep(select_v, noStyle_v, linkIfAvailable_v, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "editing",
        HelpUrl = "https://help.claris.com/en/pro-help/content/paste.html",
        // Canonical 048-Paste child order is NoStyle, SelectAll, LinkAvail;
        // <Field> follows and is omitted until a target is bound (Optional).
        Shape =
        [
            new BoolStateChild("NoStyle") { PocoProperty = "NoStyle", HrLabel = "No style", ValidValues = ["On", "Off"], DefaultValue = "False" },
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "False" },
            new BoolStateChild("LinkAvail") { PocoProperty = "LinkIfAvailable", HrLabel = "Link if available", ValidValues = ["On", "Off"], DefaultValue = "False" },
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Table::Field", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
