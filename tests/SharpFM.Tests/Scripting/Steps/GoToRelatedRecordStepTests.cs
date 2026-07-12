using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GoToRelatedRecordStepTests
{
    // Canonical (skill): no trailing <Animation> element for this step.
    private const string CanonicalXml = """
        <Step enable="True" id="74" name="Go to Related Record"><Option state="False" /><MatchAllRecords state="False" /><ShowInNewWindow state="False" /><Restore state="True" /><LayoutDestination value="SelectedLayout" /><NewWndStyles Style="Document" Close="Yes" Minimize="Yes" Maximize="Yes" Resize="Yes" Styles="3606018" /><Table id="5" name="Customers" /><Layout id="10" name="Customer Detail" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GoToRelatedRecordStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Go to Related Record", out var metadata));
        Assert.Equal(74, metadata!.Id);
    }
}
