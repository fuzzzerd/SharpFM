using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ExportFieldContentsStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="132" name="Export Field Contents"><CreateDirectories state="True" /><AutoOpen state="False" /><CreateEmail state="False" /><UniversalPathList>$path</UniversalPathList><Field table="Customer" id="5" name="photo" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ExportFieldContentsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Export Field Contents", out var metadata));
        Assert.Equal(132, metadata!.Id);
    }
}
