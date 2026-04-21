using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertEmbeddingStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="215" name="Insert Embedding"><Field table="Results" id="1" name="vec" /><LLMEmbedding><AccountName><Calculation><![CDATA["account_name"]]></Calculation></AccountName><Model><Calculation><![CDATA["model"]]></Calculation></Model><InputText><Calculation><![CDATA["input"]]></Calculation></InputText></LLMEmbedding></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertEmbeddingStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert Embedding", out var metadata));
        Assert.Equal(215, metadata!.Id);
    }
}
