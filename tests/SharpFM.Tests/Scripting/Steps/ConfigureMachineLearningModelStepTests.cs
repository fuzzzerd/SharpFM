using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConfigureMachineLearningModelStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="202" name="Configure Machine Learning Model"><Operation value="Load" /><ModelName><Calculation><![CDATA["model"]]></Calculation></ModelName><Field table="Data" id="1" name="input" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConfigureMachineLearningModelStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Configure Machine Learning Model", out var metadata));
        Assert.Equal(202, metadata!.Id);
    }
}
