using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RefreshObjectStep : ScriptStep, IStepFactory
{
    public const int XmlId = 167;
    public const string XmlName = "Refresh Object";

    public Calculation ObjectName { get; set; } = new("");
    public Calculation Repetition { get; set; } = new("");

    private RefreshObjectStep() : base(false) { }

    public RefreshObjectStep(
        Calculation? objectName = null,
        Calculation? repetition = null,
        bool enabled = true)
        : base(enabled)
    {
        ObjectName = objectName ?? new Calculation("");
        Repetition = repetition ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Refresh Object [ " + "Object Name: " + ObjectName.Text + " ; " + "Repetition: " + Repetition.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<RefreshObjectStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? objectName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Object Name:", StringComparison.OrdinalIgnoreCase)) { objectName_v = new Calculation(tok.Substring(12).Trim()); break; } }
        Calculation? repetition_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Repetition:", StringComparison.OrdinalIgnoreCase)) { repetition_v = new Calculation(tok.Substring(11).Trim()); break; } }
        return new RefreshObjectStep(objectName_v, repetition_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/refresh-object.html",
        // ObjectName and Repetition wrappers are omitted by the unconfigured
        // form (Optional).
        Shape =
        [
            new NamedCalcChild("ObjectName") { PocoProperty = "ObjectName", HrLabel = "Object Name", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("Repetition") { PocoProperty = "Repetition", HrLabel = "Repetition", Optional = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
