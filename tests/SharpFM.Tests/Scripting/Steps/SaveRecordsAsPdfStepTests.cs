using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SaveRecordsAsPdfStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="144" name="Save Records as PDF"><NoInteract state="True" /><Option state="False" /><CreateDirectories state="True" /><Restore state="True" /><AutoOpen state="False" /><CreateEmail state="False" /><UniversalPathList>$output</UniversalPathList><Calculation><![CDATA["title"]]></Calculation><PDFOptions source="RecordsBeingBrowsed"><Document><Title><Calculation><![CDATA["title"]]></Calculation></Title><Subject><Calculation><![CDATA["subject"]]></Calculation></Subject><Author><Calculation><![CDATA["author"]]></Calculation></Author><Keywords><Calculation><![CDATA["keywords"]]></Calculation></Keywords><Pages AllPages="True"><NumberFrom><Calculation><![CDATA[1]]></Calculation></NumberFrom></Pages></Document><Security allowScreenReader="True" enableCopying="True" controlEditing="AnyExceptExtractingPages" controlPrinting="HighResolution" requireControlEditPassword="False" requireOpenPassword="False" /><View magnification="100" pageLayout="SinglePage" show="PagesPanelAndPage" /></PDFOptions></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SaveRecordsAsPdfStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Save Records as PDF", out var metadata));
        Assert.Equal(144, metadata!.Id);
    }
}
