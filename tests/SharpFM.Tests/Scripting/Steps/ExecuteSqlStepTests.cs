using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ExecuteSqlStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="117" name="Execute SQL"><NoInteract state="True" /><Profile QueryType="Query" flags="0" password="" UserName="" dsn="" FieldDelimiter="&#9;" IsPredefined="-1" FieldNameRow="0" DataType="ODBC"><Query>$sql_statement</Query></Profile></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ExecuteSqlStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Execute SQL", out var metadata));
        Assert.Equal(117, metadata!.Id);
    }
}
