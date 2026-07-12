using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ClosePdfStepTests
{
    private const string MinimalXml =
        """<Step enable="True" id="245" name="Close PDF"><CreateDirectories state="False" /><AutoOpen state="False" /><CreateEmail state="False" /><ClosePDFFile><PDFSaveType>File</PDFSaveType></ClosePDFFile></Step>""";

    private const string PathXml =
        """<Step enable="True" id="245" name="Close PDF"><CreateDirectories state="False" /><AutoOpen state="True" /><CreateEmail state="True" /><UniversalPathList>file:"filename.pdf"</UniversalPathList><ClosePDFFile><PDFSaveType>File</PDFSaveType></ClosePDFFile></Step>""";

    [Theory]
    [InlineData(MinimalXml)]
    [InlineData(PathXml)]
    public void RoundTrip_CanonicalXml_IsPreserved(string canonicalXml)
    {
        var source = XElement.Parse(canonicalXml);
        var step = ClosePdfStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Close PDF", out var metadata));
        Assert.Equal(245, metadata!.Id);
    }
}
