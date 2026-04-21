using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ReadFromDataFileStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="193" name="Read from Data File"><DataSourceType value="3" /><Calculation><![CDATA[$fileID]]></Calculation><Text /><Field>$variable</Field><Count><Calculation><![CDATA[1024]]></Calculation></Count></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ReadFromDataFileStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Read from Data File", out var metadata));
        Assert.Equal(193, metadata!.Id);
    }
}
