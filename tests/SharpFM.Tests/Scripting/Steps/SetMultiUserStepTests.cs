using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SetMultiUserStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="84" name="Set Multi-User"><MultiUser value="True"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SetMultiUserStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHrMappedValue()
    {
        var step = SetMultiUserStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Set Multi-User [ Network access: On ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesHrValueBack()
    {
        var step = SetMultiUserStep.Metadata.FromDisplay!(true, new[] { "Network access: On" });
        Assert.True(XNode.DeepEquals(XElement.Parse(CanonicalXml), step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Set Multi-User", out var metadata));
        Assert.Equal(84, metadata!.Id);
        Assert.Single(metadata.Params);
        Assert.Equal("enum", metadata.Params[0].Type);
    }
}
