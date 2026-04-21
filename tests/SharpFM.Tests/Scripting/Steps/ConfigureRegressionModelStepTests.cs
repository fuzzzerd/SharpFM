using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConfigureRegressionModelStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="222" name="Configure Regression Model"><Operation value="Train" /><Field table="Data" id="1" name="target" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConfigureRegressionModelStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Configure Regression Model", out var metadata));
        Assert.Equal(222, metadata!.Id);
    }
}
