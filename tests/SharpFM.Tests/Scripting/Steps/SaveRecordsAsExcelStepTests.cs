using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SaveRecordsAsExcelStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="143" name="Save Records as Excel"><NoInteract state="True" /><CreateDirectories state="True" /><Restore state="True" /><AutoOpen state="False" /><CreateEmail state="False" /><Profile FieldDelimiter="&#9;" IsPredefined="-1" FieldNameRow="-1" DataType="XLXE" /><UniversalPathList>$path</UniversalPathList><WorkSheet><Calculation><![CDATA["Sheet1"]]></Calculation></WorkSheet><Title><Calculation><![CDATA["title"]]></Calculation></Title><Subject><Calculation><![CDATA["subject"]]></Calculation></Subject><Author><Calculation><![CDATA["author"]]></Calculation></Author><SaveType value="BrowsedRecords" /><UseFieldNames state="False" /></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SaveRecordsAsExcelStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Save Records as Excel", out var metadata));
        Assert.Equal(143, metadata!.Id);
    }
}
