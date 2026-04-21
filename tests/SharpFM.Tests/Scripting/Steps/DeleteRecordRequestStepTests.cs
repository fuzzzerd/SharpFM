using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class DeleteRecordRequestStepTests
{
    private const string TrueStateXml = """<Step enable="True" id="9" name="Delete Record/Request"><NoInteract state="True"/></Step>""";
    private const string FalseStateXml = """<Step enable="True" id="9" name="Delete Record/Request"><NoInteract state="False"/></Step>""";

    [Fact]
    public void RoundTrip_True_IsPreserved()
    {
        var source = XElement.Parse(TrueStateXml);
        var step = DeleteRecordRequestStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void RoundTrip_False_IsPreserved()
    {
        var source = XElement.Parse(FalseStateXml);
        var step = DeleteRecordRequestStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsExpectedFormat()
    {
        // Setting underlying prop=true renders as "Off"
        // (invertedHr: XML True displays as Off).
        var stepTrue = ((DeleteRecordRequestStep)DeleteRecordRequestStep.Metadata.FromXml!(XElement.Parse(TrueStateXml)));
        Assert.Equal("Delete Record/Request [ With dialog: Off ]", stepTrue.ToDisplayLine());

        var stepFalse = ((DeleteRecordRequestStep)DeleteRecordRequestStep.Metadata.FromXml!(XElement.Parse(FalseStateXml)));
        Assert.Equal("Delete Record/Request [ With dialog: On ]", stepFalse.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Delete Record/Request", out var metadata));
        Assert.Equal(9, metadata!.Id);
        Assert.Single(metadata.Params);
    }
}
