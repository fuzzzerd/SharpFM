using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class WriteToDataFileStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="192" name="Write to Data File"><AppendLineFeed state="True" /><DataSourceType value="2" /><Calculation><![CDATA[$fileID]]></Calculation><Text /><Field>$variable</Field></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = WriteToDataFileStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Write to Data File", out var metadata));
        Assert.Equal(192, metadata!.Id);
    }
}
