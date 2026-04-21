using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SendDDEExecuteStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="64" name="Send DDE Execute"><ContentType value="File"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SendDDEExecuteStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHrMappedValue()
    {
        var step = SendDDEExecuteStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Send DDE Execute [ File ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesHrValueBack()
    {
        var step = SendDDEExecuteStep.Metadata.FromDisplay!(true, new[] { "File" });
        Assert.True(XNode.DeepEquals(XElement.Parse(CanonicalXml), step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Send DDE Execute", out var metadata));
        Assert.Equal(64, metadata!.Id);
        Assert.Single(metadata.Params);
        Assert.Equal("enum", metadata.Params[0].Type);
    }
}
