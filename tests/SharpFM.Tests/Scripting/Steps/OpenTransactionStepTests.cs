using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Tests.Scripting.Steps;

public class OpenTransactionStepTests
{
    // Canonical order is Option, ESSForceCommit, SkipAutoEntry, Restore;
    // the Restore flag is part of the canonical form and round-trips.
    private const string CanonicalXml = """
        <Step enable="True" id="205" name="Open Transaction"><Option state="True"/><ESSForceCommit state="True"/><SkipAutoEntry state="True"/><Restore state="False"/></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenTransactionStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = OpenTransactionStep.Parse(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();

        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = StepDisplayFactory.TryCreate(OpenTransactionStep.XmlName, true, tokens)!;
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void RestoreElement_IsPartOfCanonicalForm_AndRoundTrips()
    {
        // The canonical form emits <Restore> in the trailing position; it
        // round-trips on both read and write.
        var withRestore = XElement.Parse("""
            <Step enable="True" id="205" name="Open Transaction"><Option state="False"/><ESSForceCommit state="False"/><SkipAutoEntry state="False"/><Restore state="False"/></Step>
            """);
        var step = OpenTransactionStep.Parse(withRestore);
        Assert.NotNull(step.ToXml().Element("Restore"));
        Assert.True(XNode.DeepEquals(withRestore, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Transaction", out var metadata));
        Assert.Equal(205, metadata!.Id);
        Assert.Equal(3, ShapeHrView.HrNodes(metadata.Shape).Count);
    }
}
