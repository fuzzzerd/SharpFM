using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ExecuteFileMakerDataApiStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="203" name="Execute FileMaker Data API"><SelectAll state="True" /><Calculation><![CDATA[$query]]></Calculation><Text /><Field>$response</Field></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ExecuteFileMakerDataApiStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Execute FileMaker Data API", out var metadata));
        Assert.Equal(203, metadata!.Id);
    }
}
