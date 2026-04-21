using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PrintSetupStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="42" name="Print Setup"><NoInteract state="True" /><Restore state="True" /><PageFormat PageOrientation="Portrait" ScaleFactor="1" PrintableHeight="734" PrintableWidth="576" PaperRight="594" PaperBottom="774" PaperLeft="-18" PaperTop="-18"><PlatformData PlatformType="M_PM"><![CDATA[]]></PlatformData><PlatformData PlatformType="MMod"><![CDATA[]]></PlatformData><PlatformData PlatformType="MMod"><![CDATA[]]></PlatformData></PageFormat></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PrintSetupStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Print Setup", out var metadata));
        Assert.Equal(42, metadata!.Id);
    }
}
