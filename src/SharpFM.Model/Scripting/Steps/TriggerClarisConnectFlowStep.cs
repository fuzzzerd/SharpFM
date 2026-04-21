using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Trigger Claris Connect Flow. The agentic-fm catalog records this step
/// with <c>id: null</c> (unconfirmed). Real FM Pro clipboard output may
/// use a different id; we default to 0 and preserve whatever id appears
/// on the source XML when round-tripping.
/// </summary>
public sealed class TriggerClarisConnectFlowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 0;
    public const string XmlName = "Trigger Claris Connect Flow";

    public bool WithDialog { get; set; }
    public bool DontEncodeUrl { get; set; }
    public bool SelectAll { get; set; }
    public bool VerifySslCertificates { get; set; }
    public string FlowUrl { get; set; }
    public Calculation? CurlOptions { get; set; }
    public string TargetVariable { get; set; }

    public TriggerClarisConnectFlowStep(
        bool withDialog = true,
        bool dontEncodeUrl = false,
        bool selectAll = true,
        bool verifySslCertificates = false,
        string flowUrl = "",
        Calculation? curlOptions = null,
        string targetVariable = "",
        bool enabled = true)
        : base(null, enabled)
    {
        WithDialog = withDialog;
        DontEncodeUrl = dontEncodeUrl;
        SelectAll = selectAll;
        VerifySslCertificates = verifySslCertificates;
        FlowUrl = flowUrl;
        CurlOptions = curlOptions;
        TargetVariable = targetVariable;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "True" : "False")),
            new XElement("DontEncodeURL", new XAttribute("state", DontEncodeUrl ? "True" : "False")),
            new XElement("SelectAll", new XAttribute("state", SelectAll ? "True" : "False")),
            new XElement("VerifySSLCertificates", new XAttribute("state", VerifySslCertificates ? "True" : "False")),
            new XElement("Flow", FlowUrl));
        if (CurlOptions is not null)
            step.Add(new XElement("CURLOptions", CurlOptions.ToXml("Calculation")));
        step.Add(new XElement("Text", TargetVariable));
        return step;
    }

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

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value == "True";
        var dontEncode = step.Element("DontEncodeURL")?.Attribute("state")?.Value == "True";
        var selectAll = step.Element("SelectAll")?.Attribute("state")?.Value == "True";
        var verify = step.Element("VerifySSLCertificates")?.Attribute("state")?.Value == "True";
        var flowUrl = step.Element("Flow")?.Value ?? "";
        var curlEl = step.Element("CURLOptions")?.Element("Calculation");
        var curl = curlEl is not null ? Calculation.FromXml(curlEl) : null;
        var text = step.Element("Text")?.Value ?? "";
        return new TriggerClarisConnectFlowStep(withDialog, dontEncode, selectAll, verify, flowUrl, curl, text, enabled);
    }

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
