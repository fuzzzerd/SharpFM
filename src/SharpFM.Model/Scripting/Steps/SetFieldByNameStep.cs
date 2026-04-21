using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetFieldByNameStep : ScriptStep, IStepFactory
{
    public const int XmlId = 147;
    public const string XmlName = "Set Field By Name";

    public Calculation TargetFieldName { get; set; }
    public Calculation CalculatedResult { get; set; }

    public SetFieldByNameStep(
        Calculation? targetFieldName = null,
        Calculation? calculatedResult = null,
        bool enabled = true)
        : base(enabled)
    {
        TargetFieldName = targetFieldName ?? new Calculation("");
        CalculatedResult = calculatedResult ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("TargetName", TargetFieldName.ToXml("Calculation")),
            new XElement("Result", CalculatedResult.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Set Field By Name [ " + "Target field name: " + TargetFieldName.Text + " ; " + "Calculated result: " + CalculatedResult.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var targetFieldName_vWrapEl = step.Element("TargetName");
        var targetFieldName_vCalcEl = targetFieldName_vWrapEl?.Element("Calculation");
        var targetFieldName_v = targetFieldName_vCalcEl is not null ? Calculation.FromXml(targetFieldName_vCalcEl) : new Calculation("");
        var calculatedResult_vWrapEl = step.Element("Result");
        var calculatedResult_vCalcEl = calculatedResult_vWrapEl?.Element("Calculation");
        var calculatedResult_v = calculatedResult_vCalcEl is not null ? Calculation.FromXml(calculatedResult_vCalcEl) : new Calculation("");
        return new SetFieldByNameStep(targetFieldName_v, calculatedResult_v, enabled);
    }

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
