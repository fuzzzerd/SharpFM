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

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PasteStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<PasteStep>(enabled, hrParams, Metadata);

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
            // Select leads the display line (Native renders before Augmented)
            // while shape order stays the canonical NoStyle, SelectAll, LinkAvail.
            new BoolStateChild("NoStyle") { PocoProperty = "NoStyle", HrLabel = "No style", ValidValues = ["On", "Off"], DefaultValue = "False", Display = DisplayMode.Augmented },
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "False", Display = DisplayMode.Native },
            new BoolStateChild("LinkAvail") { PocoProperty = "LinkIfAvailable", HrLabel = "Link if available", ValidValues = ["On", "Off"], DefaultValue = "False", Display = DisplayMode.Augmented },
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Table::Field", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
