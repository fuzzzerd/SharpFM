using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GoToObjectStep : ScriptStep, IStepFactory
{
    public const int XmlId = 145;
    public const string XmlName = "Go to Object";

    public Calculation Calculation { get; set; } = new("");
    public Calculation Calculation2 { get; set; } = new("");

    private GoToObjectStep() : base(false) { }

    public GoToObjectStep(
        Calculation? calculation = null,
        Calculation? calculation2 = null,
        bool enabled = true)
        : base(enabled)
    {
        Calculation = calculation ?? new Calculation("");
        Calculation2 = calculation2 ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Go to Object [ " + Calculation.Text + " ; " + Calculation2.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<GoToObjectStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation_v = new Calculation(tok); break; } }
        Calculation? calculation2_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation2_v = new Calculation(tok); break; } }
        return new GoToObjectStep(calculation_v, calculation2_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-object.html",
        // ObjectName and Repetition wrappers are omitted by the unconfigured
        // form (Optional).
        Shape =
        [
            new NamedCalcChild("ObjectName") { PocoProperty = "Calculation", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("Repetition") { PocoProperty = "Calculation2", Optional = true, Display = DisplayMode.Native },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
