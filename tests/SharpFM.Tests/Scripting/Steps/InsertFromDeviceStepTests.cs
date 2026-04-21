using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertFromDeviceStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="161" name="Insert from Device"><InsertFrom value="Camera" /><Field table="Products" id="1" name="photo" /><DeviceOptions><Camera choice="Back" /><Resolution choice="Full" /></DeviceOptions></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertFromDeviceStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert from Device", out var metadata));
        Assert.Equal(161, metadata!.Id);
    }
}
