using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class CommitRecordsRequestsStepTests
{
    // Canonical FM Pro clipboard form — NoInteract=True means no dialog,
    // which surfaces in display as "With dialog: Off".
    private const string NoDialogXml = """
        <Step enable="True" id="75" name="Commit Records/Requests"><NoInteract state="True"/><Option state="False"/><ESSForceCommit state="False"/></Step>
        """;

    private const string WithDialogXml = """
        <Step enable="True" id="75" name="Commit Records/Requests"><NoInteract state="False"/><Option state="False"/><ESSForceCommit state="False"/></Step>
        """;

    [Fact]
    public void RoundTrip_NoDialog_IsPreserved()
    {
        var source = XElement.Parse(NoDialogXml);
        var step = CommitRecordsRequestsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_WithDialog_IsPreserved()
    {
        var source = XElement.Parse(WithDialogXml);
        var step = CommitRecordsRequestsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void InvertedHr_Display_Correct()
    {
        // NoInteract=True (XML) -> "With dialog: Off" (display).
        var step = (CommitRecordsRequestsStep)CommitRecordsRequestsStep.Metadata.FromXml!(XElement.Parse(NoDialogXml));
        Assert.False(step.WithDialog);
        Assert.StartsWith("Commit Records/Requests [ With dialog: Off", step.ToDisplayLine());
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = CommitRecordsRequestsStep.Metadata.FromXml!(XElement.Parse(NoDialogXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = CommitRecordsRequestsStep.Metadata.FromDisplay!(true, tokens);
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Commit Records/Requests", out var metadata));
        Assert.Equal(75, metadata!.Id);
        Assert.Equal(3, metadata.Params.Count);
    }
}
