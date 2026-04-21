using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InstallMenuSetStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="142" name="Install Menu Set"><UseAsFileDefault state="False" /><CustomMenuSet id="1" name="[Standard FileMaker Menus]" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InstallMenuSetStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsMenuSetAndFlag()
    {
        var step = (InstallMenuSetStep)InstallMenuSetStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Install Menu Set [ \"[Standard FileMaker Menus]\" ; Use as file default: Off ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Install Menu Set", out var metadata));
        Assert.Equal(142, metadata!.Id);
    }
}
