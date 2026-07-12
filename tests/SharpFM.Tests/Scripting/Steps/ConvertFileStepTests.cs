using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Tests.Scripting.Steps;

public class ConvertFileStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="139" name="Convert File"><NoInteract state="False"/><Option state="False"/><SkipIndexes state="False"/><VerifySSLCertificates state="True"/></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConvertFileStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = ConvertFileStep.Parse(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = StepDisplayFactory.TryCreate(ConvertFileStep.XmlName, true, tokens)!;
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void DataSourceType_Unlabeled_RoundTripsByPosition()
    {
        var xml = XElement.Parse("""
            <Step enable="True" id="139" name="Convert File"><NoInteract state="False"/><Option state="False"/><SkipIndexes state="False"/><VerifySSLCertificates state="False"/><DataSourceType value="XMLSource"/></Step>
            """);
        var step = ConvertFileStep.Parse(xml);
        Assert.Equal("XMLSource", step.DataSourceType);
        Assert.True(XNode.DeepEquals(xml, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Convert File", out var metadata));
        Assert.Equal(139, metadata!.Id);
        Assert.Equal(5, ShapeHrView.HrNodes(metadata.Shape).Count);
    }
}
