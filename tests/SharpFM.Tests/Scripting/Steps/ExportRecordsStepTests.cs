using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ExportRecordsStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="36" name="Export Records"><NoInteract state="True" /><CreateDirectories state="True" /><Restore state="True" /><AutoOpen state="True" /><CreateEmail state="True" /><Profile FieldDelimiter="&#9;" IsPredefined="-1" FieldNameRow="-1" DataType="TABS" /><UniversalPathList>$path</UniversalPathList><ExportOptions FormatUsingCurrentLayout="False" CharacterSet="UTF-8" /><ExportEntries><ExportEntry><Field table="Customer" id="1" name="name" /></ExportEntry><ExportEntry><Field table="Customer" id="2" name="email" /></ExportEntry></ExportEntries><SummaryFields><Field GroupByFieldIsSelected="True" table="Customer" id="3" name="state" /></SummaryFields></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ExportRecordsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void ExportEntries_AreTyped()
    {
        var step = (ExportRecordsStep)ExportRecordsStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal(2, step.ExportEntries.Count);
        Assert.Equal("name", step.ExportEntries[0].Field.Name);
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Export Records", out var metadata));
        Assert.Equal(36, metadata!.Id);
    }
}
