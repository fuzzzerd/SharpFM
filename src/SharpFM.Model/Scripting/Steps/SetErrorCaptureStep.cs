using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

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

    private SetErrorCaptureStep() : base(false) { }

    public SetErrorCaptureStep(bool captureErrors = false, bool enabled = true)
        : base(enabled)
    {
        CaptureErrors = captureErrors;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Set Error Capture [ {(CaptureErrors ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetErrorCaptureStep>(step, Metadata);

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
        Shape =
        [
            new BoolStateChild("Set")
            {
                PocoProperty = "CaptureErrors",
                DefaultValue = "True",
                Description = "\"True\" (On) suppresses FileMaker Pro alert messages and some "
                    + "dialog boxes. \"False\" (Off) reenables the alert messages.",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
