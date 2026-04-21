using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ImportRecordsStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="35" name="Import Records"><NoInteract state="True" /><Restore state="True" /><VerifySSLCertificates state="False" /><DataSourceType value="File" /><UniversalPathList>$path</UniversalPathList><ImportOptions CharacterSet="UTF-8" PreserveContainer="False" MatchFieldNames="False" AutoEnter="True" SplitRepetitions="False" method="Add" /><Table id="5" name="Customer" /><TargetFields><Field FieldOptions="0" map="Import" id="1" name="name" /><Field FieldOptions="0" map="DoNotImport" id="2" name="note" /></TargetFields></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ImportRecordsStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void TargetFields_AreTyped()
    {
        var step = (ImportRecordsStep)ImportRecordsStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal(2, step.TargetFields.Count);
        Assert.Equal("Import", step.TargetFields[0].Map);
        Assert.Equal("DoNotImport", step.TargetFields[1].Map);
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Import Records", out var metadata));
        Assert.Equal(35, metadata!.Id);
    }
}
