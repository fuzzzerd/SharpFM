using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class NewWindowStepTests
{
    // Canonical (skill): LayoutDestination, optional Name, the NewWndStyles
    // attribute element (NewWindowStyles value type), optional Layout. Was a
    // mechanically-generated form with a text-bodied <NewWndStyles> and always-
    // emitted empty dimension elements.
    private const string CanonicalXml = """<Step enable="True" id="122" name="New Window"><LayoutDestination value="SelectedLayout"/><Name><Calculation><![CDATA["win"]]></Calculation></Name><NewWndStyles Style="Document" Close="Yes" Minimize="Yes" Maximize="Yes" Resize="Yes" Styles="3606018"/><Layout id="5" name="Detail"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = NewWindowStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("New Window", out var metadata));
        Assert.Equal(122, metadata!.Id);
    }
}
