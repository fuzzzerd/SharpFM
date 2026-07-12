using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class CreatePdfStepTests
{
    private const string PageRangeXml =
        """<Step enable="True" id="243" name="Create PDF"><Restore state="False" /><CreatePDFFile><Document><Pages AllPages="True"><NumberFrom><Calculation><![CDATA[1]]></Calculation></NumberFrom><PageRange><From><Calculation><![CDATA[1]]></Calculation></From><To><Calculation><![CDATA[1]]></Calculation></To></PageRange></Pages></Document><Security allowScreenReader="True" enableCopying="True" controlEditing="AnyExceptExtractingPages" controlPrinting="HighResolution" requireControlEditPassword="False" requireOpenPassword="False" /><View magnification="100" pageLayout="SinglePage" show="PagesPanelAndPage" /></CreatePDFFile></Step>""";

    private const string MetadataAndSecurityXml =
        """<Step enable="True" id="243" name="Create PDF"><Restore state="True" /><Calculation><![CDATA[1]]></Calculation><CreatePDFFile><Document><Title><Calculation><![CDATA[title expression]]></Calculation></Title><Subject><Calculation><![CDATA[subject expression]]></Calculation></Subject><Author><Calculation><![CDATA[author expression]]></Calculation></Author><Keywords><Calculation><![CDATA[keywords expression]]></Calculation></Keywords><Pages AllPages="True"><NumberFrom><Calculation><![CDATA[1]]></Calculation></NumberFrom></Pages></Document><Security allowScreenReader="True" enableCopying="True" controlEditing="InsertingDeletingRotatingPages" controlPrinting="LowResolution" requireControlEditPassword="True" requireOpenPassword="True"><OpenPassword><Calculation><![CDATA[password expression]]></Calculation></OpenPassword><ControlPassword><Calculation><![CDATA[password expression]]></Calculation></ControlPassword></Security><View magnification="100" pageLayout="SinglePage" show="PagesPanelAndPage" /></CreatePDFFile></Step>""";

    [Theory]
    [InlineData(PageRangeXml)]
    [InlineData(MetadataAndSecurityXml)]
    public void RoundTrip_CanonicalXml_IsPreserved(string canonicalXml)
    {
        var source = XElement.Parse(canonicalXml);
        var step = CreatePdfStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Create PDF", out var metadata));
        Assert.Equal(243, metadata!.Id);
    }
}
