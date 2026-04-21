using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class FindMatchingRecordsStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="155" name="Find Matching Records"><FindMatchingRecordsByField value="FindMatchingConstrain" /><Field table="Customer" id="1" name="state" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = FindMatchingRecordsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsModeAndField()
    {
        var step = (FindMatchingRecordsStep)FindMatchingRecordsStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Find Matching Records [ Constrain ; Customer::state (#1) ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Find Matching Records", out var metadata));
        Assert.Equal(155, metadata!.Id);
    }
}
