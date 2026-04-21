using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SortRecordsStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="39" name="Sort Records"><NoInteract state="True" /><Restore state="True" /><SortList Maintain="True" value="True"><Sort type="Ascending"><PrimaryField><Field table="Customer" id="1" name="name" /></PrimaryField></Sort></SortList></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SortRecordsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void SortList_IsTyped()
    {
        var step = (SortRecordsStep)SortRecordsStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.NotNull(step.Sort);
        Assert.Single(step.Sort!.Fields);
        Assert.Equal("Ascending", step.Sort.Fields[0].Type);
        Assert.Equal("name", step.Sort.Fields[0].PrimaryField.Name);
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Sort Records", out var metadata));
        Assert.Equal(39, metadata!.Id);
    }
}
