using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExitScriptStep : ScriptStep, IStepFactory
{
    public const int XmlId = 103;
    public const string XmlName = "Exit Script";

    public Calculation Calculation { get; set; } = new("");

    private ExitScriptStep() : base(false) { }

    public ExitScriptStep(
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
    {
        Calculation = calculation ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Exit Script [ " + Calculation.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ExitScriptStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation_v = new Calculation(tok); break; } }
        return new ExitScriptStep(calculation_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/exit-script.html",
        // The bare return Calculation is omitted by the unconfigured form (Optional).
        Shape =
        [
            new BareCalcChild { PocoProperty = "Calculation", Optional = true, Display = DisplayMode.Native },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
