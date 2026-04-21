using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class OpenFileStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="33" name="Open File"><Option state="False" /><FileReference id="0" name=""><UniversalPathList>file:NameOfFile</UniversalPathList></FileReference></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = OpenFileStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsHiddenFlagAndFileName()
    {
        var step = (OpenFileStep)OpenFileStep.Metadata.FromXml!(XElement.Parse(
            "<Step enable=\"True\" id=\"33\" name=\"Open File\"><Option state=\"True\"/><FileReference id=\"0\" name=\"Books\"><UniversalPathList>file:Books</UniversalPathList></FileReference></Step>"));
        Assert.Equal("Open File [ Open hidden: On ; \"Books\" ]", step.ToDisplayLine());
    }

    [Fact]
    public void Display_WithoutFile_OmitsFileToken()
    {
        var step = (OpenFileStep)OpenFileStep.Metadata.FromXml!(XElement.Parse(
            "<Step enable=\"True\" id=\"33\" name=\"Open File\"><Option state=\"False\"/></Step>"));
        Assert.Equal("Open File [ Open hidden: Off ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open File", out var metadata));
        Assert.Equal(33, metadata!.Id);
    }
}
