using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PrintStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="43" name="Print"><NoInteract state="True" /><Restore state="True" /><PrintSettings PageNumberingOffset="0" PrintToFile="False" AllPages="True" collated="True" NumCopies="1" PrintType="BrowsedRecords"><PlatformData PlatformType="PrNm"><![CDATA[]]></PlatformData><PlatformData PlatformType="M_PM"><![CDATA[]]></PlatformData><PlatformData PlatformType="M_PD"><![CDATA[]]></PlatformData><PlatformData PlatformType="MMod"><![CDATA[]]></PlatformData></PrintSettings></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PrintStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Print", out var metadata));
        Assert.Equal(43, metadata!.Id);
    }
}
