using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Configure AI Account (212). Canonical form (skill, AI reference): an optional
/// <c>&lt;VerifySSLCertificates&gt;</c>, then <c>&lt;LLMType value="…"/&gt;</c>,
/// then a <c>&lt;SetLLMAccount&gt;</c> wrapper holding the optional account name,
/// endpoint and API key. <see cref="VerifySSLCertificates"/> is nullable so an
/// absent flag stays distinct from a present "False".
/// </summary>
public sealed class ConfigureAIAccountStep : ScriptStep, IStepFactory
{
    public const int XmlId = 212;
    public const string XmlName = "Configure AI Account";

    public Calculation AccountName { get; set; } = new("");
    public string ModelProvider { get; set; } = "OpenAI";
    public Calculation Endpoint { get; set; } = new("");
    public bool? VerifySSLCertificates { get; set; }
    public Calculation APIKey { get; set; } = new("");

    private ConfigureAIAccountStep() : base(false) { }

    public ConfigureAIAccountStep(
        Calculation? accountName = null,
        string modelProvider = "OpenAI",
        Calculation? endpoint = null,
        bool? verifySSLCertificates = null,
        Calculation? aPIKey = null,
        bool enabled = true)
        : base(enabled)
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Configure AI Account [ " + "Account Name: " + AccountName.Text + " ; " + "Model Provider: " + ModelProviderHr(ModelProvider) + " ; " + "Endpoint: " + Endpoint.Text + " ; " + "Verify SSL Certificates: " + (VerifySSLCertificates == true ? "On" : "Off") + " ; " + "API key: " + APIKey.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ConfigureAIAccountStep>(step, Metadata);

    /// <summary>
    /// Display edits are anchor-preserved when the SSL flag is explicitly
    /// stored as False: the display renders both the absent and the
    /// explicit-False forms as "Off", so a parsed "Off" maps to absent.
    /// </summary>
    public override bool IsFullyEditable => VerifySSLCertificates != false;

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? accountName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase)) { accountName_v = new Calculation(tok.Substring(13).Trim()); break; } }
        string modelProvider_v = "OpenAI";
        foreach (var tok in tokens) { if (tok.StartsWith("Model Provider:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(15).Trim(); modelProvider_v = ModelProviderXml(v); break; } }
        Calculation? endpoint_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Endpoint:", StringComparison.OrdinalIgnoreCase)) { endpoint_v = new Calculation(tok.Substring(9).Trim()); break; } }
        // "Off" is ambiguous between absent and explicit False; it maps to
        // absent (explicit-False instances are sealed).
        bool? verifySSLCertificates_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Verify SSL Certificates:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(24).Trim(); verifySSLCertificates_v = v.Equals("On", StringComparison.OrdinalIgnoreCase) ? true : null; break; } }
        Calculation? aPIKey_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("API key:", StringComparison.OrdinalIgnoreCase)) { aPIKey_v = new Calculation(tok.Substring(8).Trim()); break; } }
        return new ConfigureAIAccountStep(accountName_v, modelProvider_v, endpoint_v, verifySSLCertificates_v, aPIKey_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        // Canonical: optional VerifySSLCertificates, then LLMType (value attr),
        // then the <SetLLMAccount> wrapper with the optional account fields.
        Shape =
        [
            new BoolStateChild("VerifySSLCertificates") { PocoProperty = "VerifySSLCertificates", HrLabel = "Verify SSL Certificates", Optional = true, Display = DisplayMode.Augmented },
            new EnumValueChild("LLMType") { PocoProperty = "ModelProvider", HrLabel = "Model Provider", DefaultValue = "OpenAI", DisplayValues = ["OpenAI", "Anthropic", "Cohere", "Custom"], Display = DisplayMode.Augmented },
            new WrapperChild("SetLLMAccount",
            [
                new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Augmented },
                new NamedCalcChild("Endpoint") { PocoProperty = "Endpoint", HrLabel = "Endpoint", Optional = true, Display = DisplayMode.Augmented },
                new NamedCalcChild("AccessAPIKey") { PocoProperty = "APIKey", HrLabel = "API key", Optional = true, Display = DisplayMode.Augmented },
            ]),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
