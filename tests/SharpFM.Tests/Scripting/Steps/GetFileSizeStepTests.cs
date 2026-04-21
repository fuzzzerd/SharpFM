using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GetFileSizeStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="189" name="Get File Size"><UniversalPathList>$path</UniversalPathList><Field table="Results" id="6" name="size" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GetFileSizeStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsPathAndTarget()
    {
        var step = (GetFileSizeStep)GetFileSizeStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Get File Size [ $path ; Target: Results::size (#6) ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Get File Size", out var metadata));
        Assert.Equal(189, metadata!.Id);
    }
}
