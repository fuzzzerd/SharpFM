using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed POCO for FileMaker's "Set Error Capture" script step. One
/// boolean param — On/Off — maps to <c>&lt;Set state="True|False"/&gt;</c>.
///
/// <para>Zero-loss audit: the step's XML state is the <c>Set state</c>
/// attribute plus the three attributes on <c>&lt;Step&gt;</c>. All four
/// are round-tripped exactly. FM Pro renders the step as
/// <c>Set Error Capture [ On|Off ]</c>; our display mirrors that. No
/// advanced-syntax extensions are required.</para>
/// </summary>
public sealed class SetErrorCaptureStep : ScriptStep, IStepFactory
{
    public const int XmlId = 86;
    public const string XmlName = "Set Error Capture";

    /// <summary>
    /// True when FileMaker should suppress alert messages and some
    /// dialog boxes so scripts can detect errors via <c>Get(LastError)</c>.
    /// </summary>
    public bool CaptureErrors { get; set; }

    public SetErrorCaptureStep(bool captureErrors = false, bool enabled = true)
        : base(null, enabled)
    {
        CaptureErrors = captureErrors;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Set",
                new XAttribute("state", CaptureErrors ? "True" : "False")));

    public override string ToDisplayLine() =>
        $"Set Error Capture [ {(CaptureErrors ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var state = step.Element("Set")?.Attribute("state")?.Value == "True";
        return new SetErrorCaptureStep(state, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var captureErrors = hrParams.Length > 0
            && hrParams[0].Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
        return new SetErrorCaptureStep(captureErrors, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-error-capture.html",
        HrSignature = "[ On|Off ]",
        Params =
        [
            new ParamMetadata
            {
                Name = "Set",
                XmlElement = "Set",
                Type = "boolean",
                XmlAttr = "state",
                ValidValues = ["On", "Off"],
                // Sourced from agentic-fm's snippet_examples/steps/control/Set Error Capture.xml
                // comment zone. Surfaces as a tooltip in hover-enabled UIs.
                Description = "\"True\" (On) suppresses FileMaker Pro alert messages and some "
                    + "dialog boxes. \"False\" (Off) reenables the alert messages.",
                DefaultValue = "True",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
