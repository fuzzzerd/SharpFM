using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConfigureRAGAccountStepTests
{
    // Canonical (skill): VerifySSLCertificates first, then a <ConfigureRAGAccount>
    // wrapper holding the account fields (was a flat, wrapper-less, mis-ordered form).
    private const string CanonicalXml = """<Step enable="True" id="227" name="Configure RAG Account "><VerifySSLCertificates state="True"/><ConfigureRAGAccount><RAGAccountName><Calculation><![CDATA[$x]]></Calculation></RAGAccountName><Endpoint><Calculation><![CDATA[$x]]></Calculation></Endpoint><AccessAPIKey><Calculation><![CDATA[$x]]></Calculation></AccessAPIKey></ConfigureRAGAccount></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConfigureRAGAccountStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = ConfigureRAGAccountStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = ConfigureRAGAccountStep.Metadata.FromDisplay!(true, tokens);
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Configure RAG Account ", out var metadata));
        Assert.Equal(227, metadata!.Id);
    }
}
