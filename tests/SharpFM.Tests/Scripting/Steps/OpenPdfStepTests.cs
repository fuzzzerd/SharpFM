using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class OpenPdfStepTests
{
    private const string MinimalXml =
        """<Step enable="True" id="246" name="Open PDF"><Option state="False" /><OpenPDFFile><PDFSaveType>File</PDFSaveType></OpenPDFFile></Step>""";

    private const string PathPasswordXml =
        """<Step enable="True" id="246" name="Open PDF"><Option state="True" /><UniversalPathList>$variable_or_path</UniversalPathList><OpenPDFFile><PDFSaveType>File</PDFSaveType><OpenPassword><Calculation><![CDATA[password expression]]></Calculation></OpenPassword></OpenPDFFile></Step>""";

    [Theory]
    [InlineData(MinimalXml)]
    [InlineData(PathPasswordXml)]
    public void RoundTrip_CanonicalXml_IsPreserved(string canonicalXml)
    {
        var source = XElement.Parse(canonicalXml);
        var step = OpenPdfStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Open PDF", out var metadata));
        Assert.Equal(246, metadata!.Id);
    }
}
