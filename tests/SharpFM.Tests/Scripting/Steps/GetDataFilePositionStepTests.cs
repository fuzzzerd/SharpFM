using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GetDataFilePositionStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="194" name="Get Data File Position"><Calculation><![CDATA[$handle]]></Calculation><Field table="Data" id="1" name="pos" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GetDataFilePositionStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsFileIdAndTarget()
    {
        var step = (GetDataFilePositionStep)GetDataFilePositionStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Get Data File Position [ File ID: $handle ; Target: Data::pos (#1) ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Get Data File Position", out var metadata));
        Assert.Equal(194, metadata!.Id);
    }
}
