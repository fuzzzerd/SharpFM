using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class DeleteFileStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="197" name="Delete File"><UniversalPathList>$path</UniversalPathList></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = DeleteFileStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsLabeledPath()
    {
        var step = (DeleteFileStep)DeleteFileStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Delete File [ Target file: $path ]", step.ToDisplayLine());
    }

    [Fact]
    public void FromDisplay_ParsesLabeledPath()
    {
        var step = (DeleteFileStep)DeleteFileStep.Metadata.FromDisplay!(true, new[] { "Target file: $path" });
        Assert.Equal("$path", step.TargetFile);
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Delete File", out var metadata));
        Assert.Equal(197, metadata!.Id);
    }
}
