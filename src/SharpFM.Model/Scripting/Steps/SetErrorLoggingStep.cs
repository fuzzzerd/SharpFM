using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetErrorLoggingStep : ScriptStep, IStepFactory
{
    public const int XmlId = 200;
    public const string XmlName = "Set Error Logging";

    public bool Logging { get; set; }
    public Calculation CustomDebugInfo { get; set; }

    public SetErrorLoggingStep(
        bool logging = false,
        Calculation? customDebugInfo = null,
        bool enabled = true)
        : base(enabled)
    {
        Logging = logging;
        CustomDebugInfo = customDebugInfo ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", Logging ? "True" : "False")),
            CustomDebugInfo.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Set Error Logging [ " + "Logging: " + (Logging ? "On" : "Off") + " ; " + "Custom debug info: " + CustomDebugInfo.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var logging_v = step.Element("Option")?.Attribute("state")?.Value == "True";
        var customDebugInfo_vEl = step.Element("Calculation");
        var customDebugInfo_v = customDebugInfo_vEl is not null ? Calculation.FromXml(customDebugInfo_vEl) : new Calculation("");
        return new SetErrorLoggingStep(logging_v, customDebugInfo_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool logging_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Logging:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(8).Trim(); logging_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? customDebugInfo_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Custom debug info:", StringComparison.OrdinalIgnoreCase)) { customDebugInfo_v = new Calculation(tok.Substring(18).Trim()); break; } }
        return new SetErrorLoggingStep(logging_v, customDebugInfo_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-error-logging.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Option",
                XmlElement = "Option",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Logging",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
                HrLabel = "Custom debug info",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
