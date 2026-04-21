using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class OpenTransactionStepTests
{
    // Restore is intentionally dropped per the zero-loss audit — it's
    // semantically fixed and carries no information. Test fixture omits
    // it so round-trip is byte-identical against what our POCO emits.
    private const string CanonicalXml = """
        <Step enable="True" id="205" name="Open Transaction"><SkipAutoEntry state="True"/><Option state="True"/><ESSForceCommit state="True"/></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenTransactionStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = OpenTransactionStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();

        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = OpenTransactionStep.Metadata.FromDisplay!(true, tokens);
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void RestoreElement_InSource_IsIntentionallyDropped()
    {
        // Upstream agentic-fm snippets include <Restore state="False"/>,
        // which we drop on both read and write.
        var withRestore = XElement.Parse("""
            <Step enable="True" id="205" name="Open Transaction"><SkipAutoEntry state="False"/><Option state="False"/><ESSForceCommit state="False"/><Restore state="False"/></Step>
            """);
        var step = OpenTransactionStep.Metadata.FromXml!(withRestore);
        Assert.Null(step.ToXml().Element("Restore"));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Transaction", out var metadata));
        Assert.Equal(205, metadata!.Id);
        Assert.Equal(3, metadata.Params.Count);
    }
}
