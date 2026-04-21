using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertEmbeddingInFoundSetStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="216" name="Insert Embedding in Found Set"><Field table="Results" id="2" name="vec" /><LLMBulkEmbedding><AccountName><Calculation><![CDATA["account_name"]]></Calculation></AccountName><Model><Calculation><![CDATA["model"]]></Calculation></Model><Field table="Input" id="1" name="text" /><Overwrite /><ContinueOnError /><ShowSummary /><Parameters><Calculation><![CDATA["parameters"]]></Calculation></Parameters></LLMBulkEmbedding></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertEmbeddingInFoundSetStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void FlagElements_PresenceMeansOn()
    {
        var step = (InsertEmbeddingInFoundSetStep)InsertEmbeddingInFoundSetStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.True(step.Overwrite);
        Assert.True(step.ContinueOnError);
        Assert.True(step.ShowSummary);
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert Embedding in Found Set", out var metadata));
        Assert.Equal(216, metadata!.Id);
    }
}
