using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class TruncateTableStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="182" name="Truncate Table"><NoInteract state="True" /><BaseTable id="131" comment="" name="Clients" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = TruncateTableStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsDialogAndTable()
    {
        var step = (TruncateTableStep)TruncateTableStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        // NoInteract state="True" in the canonical XML ⇒ dialog suppressed ⇒ "With dialog: Off".
        Assert.Equal("Truncate Table [ With dialog: Off ; Table: Clients ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Truncate Table", out var metadata));
        Assert.Equal(182, metadata!.Id);
    }
}
