using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertFileStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="131" name="Insert File"><UniversalPathList type="Embedded">$path</UniversalPathList><Text /><Field>$file</Field><DialogOptions asFile="True" enable="True"><Title><Calculation><![CDATA["Dialog title"]]></Calculation></Title><Storage type="InsertOnly" /><Compress type="WhenPossible" /><FilterList /></DialogOptions></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertFileStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert File", out var metadata));
        Assert.Equal(131, metadata!.Id);
    }
}
