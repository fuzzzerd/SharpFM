using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertCalculatedResultStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="77" name="Insert Calculated Result"><SelectAll state="True" /><Calculation><![CDATA["calculation"]]></Calculation><Text /><Field>$variable</Field></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertCalculatedResultStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsSelectTargetAndCalc()
    {
        var step = (InsertCalculatedResultStep)InsertCalculatedResultStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Insert Calculated Result [ Select ; Target: $variable ; \"calculation\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert Calculated Result", out var metadata));
        Assert.Equal(77, metadata!.Id);
    }
}
