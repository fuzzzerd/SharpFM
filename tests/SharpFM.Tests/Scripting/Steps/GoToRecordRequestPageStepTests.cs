using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GoToRecordRequestPageStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="16" name="Go to Record/Request/Page"><NoInteract state="True" /><RowPageLocation value="ByCalculation" /><Calculation><![CDATA[""]]></Calculation></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GoToRecordRequestPageStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Go to Record/Request/Page", out var metadata));
        Assert.Equal(16, metadata!.Id);
    }
}
