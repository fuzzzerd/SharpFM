using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetErrorLoggingStep : ScriptStep, IStepFactory
{
    public const int XmlId = 200;
    public const string XmlName = "Set Error Logging";

    public bool Logging { get; set; }
    public Calculation CustomDebugInfo { get; set; } = new("");

    private SetErrorLoggingStep() : base(false) { }

    public SetErrorLoggingStep(
        bool logging = false,
        Calculation? customDebugInfo = null,
        bool enabled = true)
        : base(enabled)
    {
        Logging = logging;
        CustomDebugInfo = customDebugInfo ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Set Error Logging [ " + "Logging: " + (Logging ? "On" : "Off") + " ; " + "Custom debug info: " + CustomDebugInfo.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetErrorLoggingStep>(step, Metadata);

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
        // Option flag then the bare custom-debug Calculation, which the
        // unconfigured form omits (Optional).
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "Logging", HrLabel = "Logging", Display = DisplayMode.Native },
            new BareCalcChild { PocoProperty = "CustomDebugInfo", HrLabel = "Custom debug info", Optional = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
