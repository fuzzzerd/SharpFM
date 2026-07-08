using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Configure RAG Account (227). Canonical form (skill, accounts/AI reference):
/// <c>&lt;VerifySSLCertificates state="False"/&gt;</c> followed by a
/// <c>&lt;ConfigureRAGAccount&gt;</c> wrapper that holds the account fields when
/// configured and is emitted empty when not. The step name carries a trailing
/// space, preserved verbatim.
/// </summary>
public sealed class ConfigureRAGAccountStep : ScriptStep, IStepFactory
{
    public const int XmlId = 227;
    public const string XmlName = "Configure RAG Account ";

    public Calculation RAGAccountName { get; set; } = new("");
    public Calculation Endpoint { get; set; } = new("");
    public Calculation APIKey { get; set; } = new("");
    public bool VerifySSLCertificates { get; set; }

    private ConfigureRAGAccountStep() : base(false) { }

    public ConfigureRAGAccountStep(
        Calculation? rAGAccountName = null,
        Calculation? endpoint = null,
        Calculation? aPIKey = null,
        bool verifySSLCertificates = false,
        bool enabled = true)
        : base(enabled)
    {
        RAGAccountName = rAGAccountName ?? new Calculation("");
        Endpoint = endpoint ?? new Calculation("");
        APIKey = aPIKey ?? new Calculation("");
        VerifySSLCertificates = verifySSLCertificates;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ConfigureRAGAccountStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<ConfigureRAGAccountStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        // Canonical: VerifySSLCertificates, then the <ConfigureRAGAccount> wrapper
        // holding the (optional) account fields. Unconfigured -> empty wrapper.
        Shape =
        [
            new BoolStateChild("VerifySSLCertificates") { PocoProperty = "VerifySSLCertificates", HrLabel = "Verify SSL Certificates", Display = DisplayMode.Augmented },
            new WrapperChild("ConfigureRAGAccount",
            [
                new NamedCalcChild("RAGAccountName") { PocoProperty = "RAGAccountName", HrLabel = "RAG Account Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
                new NamedCalcChild("Endpoint") { PocoProperty = "Endpoint", HrLabel = "Endpoint", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
                new NamedCalcChild("AccessAPIKey") { PocoProperty = "APIKey", HrLabel = "API key", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            ]),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
