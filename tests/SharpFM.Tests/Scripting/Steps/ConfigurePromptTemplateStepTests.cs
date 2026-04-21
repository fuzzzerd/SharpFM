using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConfigurePromptTemplateStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="226" name="Configure Prompt Template"><TemplateName><Calculation><![CDATA[$x]]></Calculation></TemplateName><ModelProvider value="OpenAI"/><RequestType value="SQL Query"/><SQLPrompt><Calculation><![CDATA[$x]]></Calculation></SQLPrompt><NaturalLanguagePrompt><Calculation><![CDATA[$x]]></Calculation></NaturalLanguagePrompt><FindRequestPrompt><Calculation><![CDATA[$x]]></Calculation></FindRequestPrompt><RAGPPrompt><Calculation><![CDATA[$x]]></Calculation></RAGPPrompt><Option state="True"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConfigurePromptTemplateStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Configure Prompt Template", out var metadata));
        Assert.Equal(226, metadata!.Id);
    }
}
