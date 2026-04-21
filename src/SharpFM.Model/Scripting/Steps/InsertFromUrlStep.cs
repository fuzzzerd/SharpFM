using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InsertFromUrlStep : ScriptStep, IStepFactory
{
    public const int XmlId = 160;
    public const string XmlName = "Insert from URL";

    public bool SelectAll { get; set; }
    public bool WithDialog { get; set; }
    public bool VerifySslCertificates { get; set; }
    public bool DontEncodeUrl { get; set; }
    public FieldRef? Target { get; set; }
    public Calculation? Url { get; set; }
    public Calculation? CurlOptions { get; set; }

    public InsertFromUrlStep(
        bool selectAll = true,
        bool withDialog = true,
        bool verifySslCertificates = false,
        bool dontEncodeUrl = false,
        FieldRef? target = null,
        Calculation? url = null,
        Calculation? curlOptions = null,
        bool enabled = true)
        : base(enabled)
    {
        SelectAll = selectAll;
        WithDialog = withDialog;
        VerifySslCertificates = verifySslCertificates;
        DontEncodeUrl = dontEncodeUrl;
        Target = target;
        Url = url;
        CurlOptions = curlOptions;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            // NoInteract is inverted: state="True" suppresses the dialog
            // (= With dialog: Off). state="False" shows it.
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("DontEncodeURL", new XAttribute("state", DontEncodeUrl ? "True" : "False")),
            new XElement("SelectAll", new XAttribute("state", SelectAll ? "True" : "False")),
            new XElement("VerifySSLCertificates", new XAttribute("state", VerifySslCertificates ? "True" : "False")));
        if (CurlOptions is not null)
            step.Add(new XElement("CURLOptions", CurlOptions.ToXml("Calculation")));
        if (Url is not null) step.Add(Url.ToXml("Calculation"));
        if (Target is not null)
        {
            if (Target.IsVariable) step.Add(new XElement("Text"));
            step.Add(Target.ToXml("Field"));
        }
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (SelectAll) parts.Add("Select");
        parts.Add($"With dialog: {(WithDialog ? "On" : "Off")}");
        if (Target is not null) parts.Add($"Target: {Target.ToDisplayString()}");
        if (Url is not null) parts.Add(Url.Text);
        if (VerifySslCertificates) parts.Add("Verify SSL Certificates");
        if (CurlOptions is not null) parts.Add($"cURL options: {CurlOptions.Text}");
        if (DontEncodeUrl) parts.Add("Don't encode URL");
        return $"Insert from URL [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        // NoInteract inverted: state="True" = dialog suppressed = WithDialog: Off.
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var dontEncode = step.Element("DontEncodeURL")?.Attribute("state")?.Value == "True";
        var selectAll = step.Element("SelectAll")?.Attribute("state")?.Value == "True";
        var verify = step.Element("VerifySSLCertificates")?.Attribute("state")?.Value == "True";
        var curlEl = step.Element("CURLOptions")?.Element("Calculation");
        var curl = curlEl is not null ? Calculation.FromXml(curlEl) : null;
        var urlEl = step.Element("Calculation");
        var url = urlEl is not null ? Calculation.FromXml(urlEl) : null;
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new InsertFromUrlStep(selectAll, withDialog, verify, dontEncode, target, url, curl, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool selectAll = false;
        bool withDialog = true;
        bool verify = false;
        bool dontEncode = false;
        FieldRef? target = null;
        Calculation? url = null;
        Calculation? curl = null;
        bool urlSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Select", StringComparison.OrdinalIgnoreCase))
                selectAll = true;
            else if (t.Equals("Verify SSL Certificates", StringComparison.OrdinalIgnoreCase))
                verify = true;
            else if (t.Equals("Don't encode URL", StringComparison.OrdinalIgnoreCase))
                dontEncode = true;
            else if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            else if (t.StartsWith("cURL options:", StringComparison.OrdinalIgnoreCase))
                curl = new Calculation(t.Substring(13).Trim());
            else if (!urlSeen && !string.IsNullOrWhiteSpace(t))
            {
                url = new Calculation(t);
                urlSeen = true;
            }
        }
        return new InsertFromUrlStep(selectAll, withDialog, verify, dontEncode, target, url, curl, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-from-url.html",
        Params =
        [
            new ParamMetadata { Name = "SelectAll", XmlElement = "SelectAll", XmlAttr = "state", Type = "boolean", HrLabel = "Select" },
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog", ValidValues = ["On", "Off"] },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "fieldOrVariable", HrLabel = "Target" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation" },
            new ParamMetadata { Name = "VerifySSLCertificates", XmlElement = "VerifySSLCertificates", XmlAttr = "state", Type = "boolean", HrLabel = "Verify SSL Certificates" },
            new ParamMetadata { Name = "CURLOptions", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "cURL options" },
            new ParamMetadata { Name = "DontEncodeURL", XmlElement = "DontEncodeURL", XmlAttr = "state", Type = "boolean" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
