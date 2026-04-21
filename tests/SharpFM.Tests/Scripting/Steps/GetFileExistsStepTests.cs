using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GetFileExistsStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="188" name="Get File Exists"><UniversalPathList>$path</UniversalPathList><Field table="Results" id="5" name="exists" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GetFileExistsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsPathAndTarget()
    {
        var step = (GetFileExistsStep)GetFileExistsStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Get File Exists [ $path ; Target: Results::exists (#5) ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Get File Exists", out var metadata));
        Assert.Equal(188, metadata!.Id);
    }
}
