using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class AppendPdfStepTests
{
    private const string MinimalXml =
        """<Step enable="True" id="244" name="Append PDF"><Option state="False" /><AppendPDFFile><PDFSaveType>File</PDFSaveType></AppendPDFFile></Step>""";

    private const string PathPasswordXml =
        """<Step enable="True" id="244" name="Append PDF"><Option state="True" /><UniversalPathList>image:../path/to/file.pdf</UniversalPathList><AppendPDFFile><PDFSaveType>File</PDFSaveType><OpenPassword><Calculation><![CDATA[password expression]]></Calculation></OpenPassword></AppendPDFFile></Step>""";

    [Theory]
    [InlineData(MinimalXml)]
    [InlineData(PathPasswordXml)]
    public void RoundTrip_CanonicalXml_IsPreserved(string canonicalXml)
    {
        var source = XElement.Parse(canonicalXml);
        var step = AppendPdfStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Append PDF", out var metadata));
        Assert.Equal(244, metadata!.Id);
    }
}
