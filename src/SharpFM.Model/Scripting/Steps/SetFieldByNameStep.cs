using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetFieldByNameStep : ScriptStep, IStepFactory
{
    public const int XmlId = 147;
    public const string XmlName = "Set Field By Name";

    public Calculation TargetFieldName { get; set; } = new("");
    public Calculation CalculatedResult { get; set; } = new("");

    private SetFieldByNameStep() : base(false) { }

    public SetFieldByNameStep(
        Calculation? targetFieldName = null,
        Calculation? calculatedResult = null,
        bool enabled = true)
        : base(enabled)
    {
        TargetFieldName = targetFieldName ?? new Calculation("");
        CalculatedResult = calculatedResult ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Set Field By Name [ " + "Target field name: " + TargetFieldName.Text + " ; " + "Calculated result: " + CalculatedResult.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetFieldByNameStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? targetFieldName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Target field name:", StringComparison.OrdinalIgnoreCase)) { targetFieldName_v = new Calculation(tok.Substring(18).Trim()); break; } }
        Calculation? calculatedResult_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Calculated result:", StringComparison.OrdinalIgnoreCase)) { calculatedResult_v = new Calculation(tok.Substring(18).Trim()); break; } }
        return new SetFieldByNameStep(targetFieldName_v, calculatedResult_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-field-by-name.html",
        // TargetName and Result wrappers are omitted by the unconfigured form (Optional).
        Shape =
        [
            new NamedCalcChild("TargetName") { PocoProperty = "TargetFieldName", HrLabel = "Target field name", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("Result") { PocoProperty = "CalculatedResult", HrLabel = "Calculated result", Optional = true, Display = DisplayMode.Native },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Target field name",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Calculated result",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
