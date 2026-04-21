using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SortRecordsByFieldStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="154" name="Sort Records by Field"><SortRecordsByField value="SortDescending" /><Field table="Customer" id="2" name="name" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SortRecordsByFieldStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsOrderAndField()
    {
        var step = (SortRecordsByFieldStep)SortRecordsByFieldStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("Sort Records by Field [ Descending ; Customer::name (#2) ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Sort Records by Field", out var metadata));
        Assert.Equal(154, metadata!.Id);
    }
}
