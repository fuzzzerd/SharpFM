using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ReplaceFieldContentsStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="91" name="Replace Field Contents"><NoInteract state="True" /><With value="Calculation" /><Calculation><![CDATA["value"]]></Calculation><SerialNumbers PerformAutoEnter="True" UpdateEntryOptions="False" UseEntryOptions="True" /><Field table="Customer" id="3" name="id" /></Step>
        """;

    // Source with Restore element (as from agentic-fm snippets) — our POCO drops it.
    private const string WithRestoreXml = """
        <Step enable="True" id="91" name="Replace Field Contents"><NoInteract state="True" /><Restore state="True" /><With value="Calculation" /><Calculation><![CDATA["value"]]></Calculation><SerialNumbers PerformAutoEnter="True" UpdateEntryOptions="False" UseEntryOptions="True" /><Field table="Customer" id="3" name="id" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ReplaceFieldContentsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RestoreElement_IsDropped()
    {
        var source = XElement.Parse(WithRestoreXml);
        var step = ReplaceFieldContentsStep.Metadata.FromXml!(source);
        var output = step.ToXml();
        Assert.Null(output.Element("Restore"));
    }

    [Fact]
    public void Display_EmitsDialogFieldAndCalculation()
    {
        var step = (ReplaceFieldContentsStep)ReplaceFieldContentsStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        // NoInteract state="True" in the canonical XML ⇒ dialog suppressed ⇒ "With dialog: Off".
        Assert.Equal("Replace Field Contents [ With dialog: Off ; Customer::id (#3) ; \"value\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Replace Field Contents", out var metadata));
        Assert.Equal(91, metadata!.Id);
    }
}
