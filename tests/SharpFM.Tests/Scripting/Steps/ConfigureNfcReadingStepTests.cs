using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConfigureNfcReadingStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="201" name="Configure NFC Reading"><Option value="Start" /><Script id="0" name="" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConfigureNfcReadingStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Configure NFC Reading", out var metadata));
        Assert.Equal(201, metadata!.Id);
    }
}
