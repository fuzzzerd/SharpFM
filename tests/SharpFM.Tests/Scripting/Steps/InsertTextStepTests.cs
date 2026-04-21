using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertTextStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="61" name="Insert Text"><SelectAll state="True" /><Text>text content</Text><Field>$variable</Field></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertTextStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsSelectTargetAndText()
    {
        var step = (InsertTextStep)InsertTextStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Insert Text [ Select ; Target: $variable ; \"text content\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert Text", out var metadata));
        Assert.Equal(61, metadata!.Id);
    }
}
