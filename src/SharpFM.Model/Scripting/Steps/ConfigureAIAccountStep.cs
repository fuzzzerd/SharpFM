using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ConfigureAIAccountStep : ScriptStep, IStepFactory
{
    public const int XmlId = 212;
    public const string XmlName = "Configure AI Account";

    public Calculation AccountName { get; set; }
    public string ModelProvider { get; set; }
    public Calculation Endpoint { get; set; }
    public bool VerifySSLCertificates { get; set; }
    public Calculation APIKey { get; set; }

    public ConfigureAIAccountStep(
        Calculation? accountName = null,
        string modelProvider = "OpenAI",
        Calculation? endpoint = null,
        bool verifySSLCertificates = false,
        Calculation? aPIKey = null,
        bool enabled = true)
        : base(null, enabled)
    {
        AccountName = accountName ?? new Calculation("");
        ModelProvider = modelProvider;
        Endpoint = endpoint ?? new Calculation("");
        VerifySSLCertificates = verifySSLCertificates;
        APIKey = aPIKey ?? new Calculation("");
    }

    private static readonly IReadOnlyDictionary<string, string> _ModelProviderToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["OpenAI"] = "OpenAI",
        ["Anthropic"] = "Anthropic",
        ["Cohere"] = "Cohere",
        ["Custom"] = "Custom",
    };
    private static readonly IReadOnlyDictionary<string, string> _ModelProviderFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["OpenAI"] = "OpenAI",
        ["Anthropic"] = "Anthropic",
        ["Cohere"] = "Cohere",
        ["Custom"] = "Custom",
    };
    private static string ModelProviderHr(string x) => _ModelProviderToHr.TryGetValue(x, out var h) ? h : x;
    private static string ModelProviderXml(string h) => _ModelProviderFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("AccoutName", AccountName.ToXml("Calculation")),
            new XElement("LLMType", new XAttribute("value", ModelProvider)),
            new XElement("Endpoint", Endpoint.ToXml("Calculation")),
            new XElement("VerifySSLCertificates", new XAttribute("state", VerifySSLCertificates ? "True" : "False")),
            new XElement("AccessAPIKey", APIKey.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Configure AI Account [ " + "Account Name: " + AccountName.Text + " ; " + "Model Provider: " + ModelProviderHr(ModelProvider) + " ; " + "Endpoint: " + Endpoint.Text + " ; " + "Verify SSL Certificates: " + (VerifySSLCertificates ? "On" : "Off") + " ; " + "API key: " + APIKey.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var accountName_vWrapEl = step.Element("AccoutName");
        var accountName_vCalcEl = accountName_vWrapEl?.Element("Calculation");
        var accountName_v = accountName_vCalcEl is not null ? Calculation.FromXml(accountName_vCalcEl) : new Calculation("");
        var modelProvider_v = step.Element("LLMType")?.Attribute("value")?.Value ?? "OpenAI";
        var endpoint_vWrapEl = step.Element("Endpoint");
        var endpoint_vCalcEl = endpoint_vWrapEl?.Element("Calculation");
        var endpoint_v = endpoint_vCalcEl is not null ? Calculation.FromXml(endpoint_vCalcEl) : new Calculation("");
        var verifySSLCertificates_v = step.Element("VerifySSLCertificates")?.Attribute("state")?.Value == "True";
        var aPIKey_vWrapEl = step.Element("AccessAPIKey");
        var aPIKey_vCalcEl = aPIKey_vWrapEl?.Element("Calculation");
        var aPIKey_v = aPIKey_vCalcEl is not null ? Calculation.FromXml(aPIKey_vCalcEl) : new Calculation("");
        return new ConfigureAIAccountStep(accountName_v, modelProvider_v, endpoint_v, verifySSLCertificates_v, aPIKey_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? accountName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase)) { accountName_v = new Calculation(tok.Substring(13).Trim()); break; } }
        string modelProvider_v = "OpenAI";
        foreach (var tok in tokens) { if (tok.StartsWith("Model Provider:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(15).Trim(); modelProvider_v = ModelProviderXml(v); break; } }
        Calculation? endpoint_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Endpoint:", StringComparison.OrdinalIgnoreCase)) { endpoint_v = new Calculation(tok.Substring(9).Trim()); break; } }
        bool verifySSLCertificates_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Verify SSL Certificates:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(24).Trim(); verifySSLCertificates_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? aPIKey_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("API key:", StringComparison.OrdinalIgnoreCase)) { aPIKey_v = new Calculation(tok.Substring(8).Trim()); break; } }
        return new ConfigureAIAccountStep(accountName_v, modelProvider_v, endpoint_v, verifySSLCertificates_v, aPIKey_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Account Name",
            },
            new ParamMetadata
            {
                Name = "LLMType",
                XmlElement = "LLMType",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Model Provider",
                ValidValues = ["OpenAI", "Anthropic", "Cohere", "Custom"],
                DefaultValue = "OpenAI",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Endpoint",
            },
            new ParamMetadata
            {
                Name = "VerifySSLCertificates",
                XmlElement = "VerifySSLCertificates",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Verify SSL Certificates",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "API key",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
