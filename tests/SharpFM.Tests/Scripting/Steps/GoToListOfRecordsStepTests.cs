using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GoToListOfRecordsStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="228" name="Go to List of Records"><ShowInNewWindow state="False" /><LayoutDestination value="CurrentLayout" /><RowList><Calculation><![CDATA["calc"]]></Calculation></RowList><NewWndStyles Style="Document" Close="Yes" Minimize="Yes" Maximize="Yes" Resize="Yes" Styles="3606018" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GoToListOfRecordsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Go to List of Records", out var metadata));
        Assert.Equal(228, metadata!.Id);
    }
}
