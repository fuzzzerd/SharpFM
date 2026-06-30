using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RevertTransactionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 207;
    public const string XmlName = "Revert Transaction";

    public bool Option { get; set; }
    public Calculation? Condition { get; set; }
    public Calculation? ErrorCode { get; set; }
    public Calculation? ErrorMessage { get; set; }

    private RevertTransactionStep() : base(false) { }

    public RevertTransactionStep(
        bool option = false,
        Calculation? condition = null,
        Calculation? errorCode = null,
        Calculation? errorMessage = null,
        bool enabled = true)
        : base(enabled)
    {
        Option = option;
        Condition = condition;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Revert Transaction [ " + (Option ? "On" : "Off") + " ; " + "Condition: " + (Condition?.Text ?? "") + " ; " + "Error Code: " + (ErrorCode?.Text ?? "") + " ; " + "Error Message: " + (ErrorMessage?.Text ?? "") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<RevertTransactionStep>(step, Metadata);

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
        // Canonical: Option, then the optional Condition / ErrorCode /
        // ErrorMessage calculations (omitted when unconfigured).
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "Option", Display = DisplayMode.Native },
            new NamedCalcChild("Condition") { PocoProperty = "Condition", HrLabel = "Condition", Optional = true, Display = DisplayMode.Augmented },
            new NamedCalcChild("ErrorCode") { PocoProperty = "ErrorCode", HrLabel = "Error Code", Optional = true, Display = DisplayMode.Augmented },
            new NamedCalcChild("ErrorMessage") { PocoProperty = "ErrorMessage", HrLabel = "Error Message", Optional = true, Display = DisplayMode.Augmented },
        ],
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
