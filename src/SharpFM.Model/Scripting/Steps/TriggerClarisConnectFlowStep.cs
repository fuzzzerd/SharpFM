using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Trigger Claris Connect Flow. Posts to a Claris Connect flow's HTTP
/// endpoint with optional cURL options and captures the response into a
/// target variable.
///
/// <para>
/// The canonical step id is 211 (vendored FileMaker XML skill, control reference).
/// </para>
/// </summary>
public sealed class TriggerClarisConnectFlowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 211;
    public const string XmlName = "Trigger Claris Connect Flow";

    public bool WithDialog { get; set; }
    public bool DontEncodeUrl { get; set; }
    public bool SelectAll { get; set; }
    public bool VerifySslCertificates { get; set; }
    public string FlowUrl { get; set; }
    public Calculation? CurlOptions { get; set; }
    public string TargetVariable { get; set; }

    // Wire adapter: the XML carries <NoInteract state>, the inverse of the
    // With-dialog option the UI (and display line) exposes. Get/set so the
    // shape renderer and parser both go through it.
    public bool NoInteract
    {
        get => !WithDialog;
        set => WithDialog = !value;
    }

    private TriggerClarisConnectFlowStep() : base(false)
    {
        FlowUrl = "";
        TargetVariable = "";
    }

    public TriggerClarisConnectFlowStep(
        bool withDialog = true,
        bool dontEncodeUrl = false,
        bool selectAll = true,
        bool verifySslCertificates = false,
        string flowUrl = "",
        Calculation? curlOptions = null,
        string targetVariable = "",
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        DontEncodeUrl = dontEncodeUrl;
        SelectAll = selectAll;
        VerifySslCertificates = verifySslCertificates;
        FlowUrl = flowUrl;
        CurlOptions = curlOptions;
        TargetVariable = targetVariable;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<TriggerClarisConnectFlowStep>(step, Metadata);

    /// <summary>
    /// Display edits are anchor-preserved when state the display line cannot
    /// carry is present: a target variable, or the encode/select/SSL flags
    /// off their unconfigured values (the display shows only the dialog
    /// toggle, flow URL, and cURL options).
    /// </summary>
    public override bool IsFullyEditable =>
        TargetVariable.Length == 0 && !DontEncodeUrl && SelectAll && !VerifySslCertificates;

    // Hand-written: reconstructs the unconfigured SelectAll=true wire default,
    // which the shape parser cannot synthesize for a display-hidden slot.
    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool withDialog = true;
        string flowUrl = "";
        Calculation? curl = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Flow URL:", StringComparison.OrdinalIgnoreCase))
                flowUrl = t.Substring(9).Trim();
            else if (t.StartsWith("cURL options:", StringComparison.OrdinalIgnoreCase))
                curl = new Calculation(t.Substring(13).Trim());
        }
        return new TriggerClarisConnectFlowStep(withDialog, false, true, false, flowUrl, curl, "", enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        Shape =
        [
            new BoolStateChild("NoInteract") { HrLabel = "With dialog", DisplayInverted = true },
            // The display line carries only the dialog toggle, flow URL, and
            // cURL options; the remaining state seals the step (IsFullyEditable).
            new BoolStateChild("DontEncodeURL") { PocoProperty = "DontEncodeUrl", HrLabel = "Don't encode URL", Display = DisplayMode.Hidden },
            new BoolStateChild("SelectAll") { HrLabel = "Select entire contents", Display = DisplayMode.Hidden },
            new BoolStateChild("VerifySSLCertificates") { PocoProperty = "VerifySslCertificates", HrLabel = "Verify SSL certificates", Display = DisplayMode.Hidden },
            new NamedTextChild("Flow") { PocoProperty = "FlowUrl", HrLabel = "Flow URL", DisplayEmptyAs = "" },
            new NamedCalcChild("CURLOptions") { PocoProperty = "CurlOptions", Optional = true, HrLabel = "cURL options" },
            new NamedTextChild("Text") { PocoProperty = "TargetVariable", HrLabel = "Target variable", Display = DisplayMode.Hidden },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
