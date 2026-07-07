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

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"With dialog: {(WithDialog ? "On" : "Off")}",
            $"Flow URL: {FlowUrl}",
        };
        if (CurlOptions is not null) parts.Add($"cURL options: {CurlOptions.Text}");
        return $"Trigger Claris Connect Flow [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<TriggerClarisConnectFlowStep>(step, Metadata);

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
            new BoolStateChild("NoInteract") { HrLabel = "With dialog" },
            new BoolStateChild("DontEncodeURL") { PocoProperty = "DontEncodeUrl", HrLabel = "Don't encode URL" },
            new BoolStateChild("SelectAll") { HrLabel = "Select entire contents" },
            new BoolStateChild("VerifySSLCertificates") { PocoProperty = "VerifySslCertificates", HrLabel = "Verify SSL certificates" },
            new NamedTextChild("Flow") { PocoProperty = "FlowUrl", HrLabel = "Flow URL" },
            new NamedCalcChild("CURLOptions") { PocoProperty = "CurlOptions", Optional = true, HrLabel = "cURL options" },
            new NamedTextChild("Text") { PocoProperty = "TargetVariable", HrLabel = "Target variable" },
        ],
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog", ValidValues = ["On", "Off"] },
            new ParamMetadata { Name = "DontEncodeURL", XmlElement = "DontEncodeURL", XmlAttr = "state", Type = "boolean", HrLabel = "Don't encode URL" },
            new ParamMetadata { Name = "SelectAll", XmlElement = "SelectAll", XmlAttr = "state", Type = "boolean", HrLabel = "Select entire contents" },
            new ParamMetadata { Name = "VerifySSLCertificates", XmlElement = "VerifySSLCertificates", XmlAttr = "state", Type = "boolean", HrLabel = "Verify SSL certificates" },
            new ParamMetadata { Name = "Flow", XmlElement = "Flow", Type = "text", HrLabel = "Flow URL" },
            new ParamMetadata { Name = "CURLOptions", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "cURL options" },
            new ParamMetadata { Name = "Text", XmlElement = "Text", Type = "text", HrLabel = "Target variable" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
