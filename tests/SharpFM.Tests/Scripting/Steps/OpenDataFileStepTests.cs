using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class OpenDataFileStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="191" name="Open Data File"><UniversalPathList>$path</UniversalPathList><Field table="Data" id="1" name="handle" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenDataFileStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsPathAndTarget()
    {
        var step = (OpenDataFileStep)OpenDataFileStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Open Data File [ $path ; Target: Data::handle (#1) ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open Data File", out var metadata));
        Assert.Equal(191, metadata!.Id);
    }
}
