using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class CloseFileStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="34" name="Close File"><FileReference id="0" name=""><UniversalPathList>file:NameOfFile</UniversalPathList></FileReference></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = CloseFileStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_BareStep_UsesCurrentFile()
    {
        var bare = XElement.Parse("<Step enable=\"True\" id=\"34\" name=\"Close File\" />");
        var step = CloseFileStep.Metadata.FromXml!(bare);
        Assert.Equal("Close File [ Current File ]", step.ToDisplayLine());
    }

    [Fact]
    public void Display_WithFileReference_UsesName()
    {
        var step = (CloseFileStep)CloseFileStep.Metadata.FromXml!(XElement.Parse(
            "<Step enable=\"True\" id=\"34\" name=\"Close File\"><FileReference id=\"0\" name=\"Employees\"><UniversalPathList>file:Employees</UniversalPathList></FileReference></Step>"));
        Assert.Equal("Close File [ \"Employees\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Close File", out var metadata));
        Assert.Equal(34, metadata!.Id);
    }
}
