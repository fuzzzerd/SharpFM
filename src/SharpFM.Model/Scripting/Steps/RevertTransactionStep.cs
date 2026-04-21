using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RevertTransactionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 207;
    public const string XmlName = "Revert Transaction";

    public bool Option { get; set; }
    public Calculation Condition { get; set; }
    public Calculation ErrorCode { get; set; }
    public Calculation ErrorMessage { get; set; }

    public RevertTransactionStep(
        bool option = false,
        Calculation? condition = null,
        Calculation? errorCode = null,
        Calculation? errorMessage = null,
        bool enabled = true)
        : base(null, enabled)
    {
        Option = option;
        Condition = condition ?? new Calculation("");
        ErrorCode = errorCode ?? new Calculation("");
        ErrorMessage = errorMessage ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", Option ? "True" : "False")),
            new XElement("Condition", Condition.ToXml("Calculation")),
            new XElement("ErrorCode", ErrorCode.ToXml("Calculation")),
            new XElement("ErrorMessage", ErrorMessage.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Revert Transaction [ " + (Option ? "On" : "Off") + " ; " + "Condition: " + Condition.Text + " ; " + "Error Code: " + ErrorCode.Text + " ; " + "Error Message: " + ErrorMessage.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var option_v = step.Element("Option")?.Attribute("state")?.Value == "True";
        var condition_vWrapEl = step.Element("Condition");
        var condition_vCalcEl = condition_vWrapEl?.Element("Calculation");
        var condition_v = condition_vCalcEl is not null ? Calculation.FromXml(condition_vCalcEl) : new Calculation("");
        var errorCode_vWrapEl = step.Element("ErrorCode");
        var errorCode_vCalcEl = errorCode_vWrapEl?.Element("Calculation");
        var errorCode_v = errorCode_vCalcEl is not null ? Calculation.FromXml(errorCode_vCalcEl) : new Calculation("");
        var errorMessage_vWrapEl = step.Element("ErrorMessage");
        var errorMessage_vCalcEl = errorMessage_vWrapEl?.Element("Calculation");
        var errorMessage_v = errorMessage_vCalcEl is not null ? Calculation.FromXml(errorMessage_vCalcEl) : new Calculation("");
        return new RevertTransactionStep(option_v, condition_v, errorCode_v, errorMessage_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool option_v = false;
        Calculation? condition_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Condition:", StringComparison.OrdinalIgnoreCase)) { condition_v = new Calculation(tok.Substring(10).Trim()); break; } }
        Calculation? errorCode_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Error Code:", StringComparison.OrdinalIgnoreCase)) { errorCode_v = new Calculation(tok.Substring(11).Trim()); break; } }
        Calculation? errorMessage_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Error Message:", StringComparison.OrdinalIgnoreCase)) { errorMessage_v = new Calculation(tok.Substring(14).Trim()); break; } }
        return new RevertTransactionStep(option_v, condition_v, errorCode_v, errorMessage_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/revert-transaction.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Option",
                XmlElement = "Option",
                Type = "flagBoolean",
                XmlAttr = "state",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Condition",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Error Code",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Error Message",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
