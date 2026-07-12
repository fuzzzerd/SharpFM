using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConfigurePromptTemplateStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="226" name="Configure Prompt Template"><Option state="False"/><ConfigurePromptTemplate><ModelProvider>ChatGPT</ModelProvider><RequestType>SQLQuery</RequestType></ConfigurePromptTemplate></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConfigurePromptTemplateStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Configure Prompt Template", out var metadata));
        Assert.Equal(226, metadata!.Id);
    }
}
