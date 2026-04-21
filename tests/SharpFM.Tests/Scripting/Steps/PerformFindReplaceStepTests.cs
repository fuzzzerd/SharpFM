using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformFindReplaceStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="128" name="Perform Find/Replace"><NoInteract state="False" /><FindReplaceOperation MatchWholeWords="True" MatchCase="True" WithinOptions="All" AcrossOptions="All" direction="Forward" type="FindNext" /><FindCalc><Calculation><![CDATA["find content"]]></Calculation></FindCalc><ReplaceCalc><Calculation><![CDATA["replace content"]]></Calculation></ReplaceCalc></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = PerformFindReplaceStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Perform Find/Replace", out var metadata));
        Assert.Equal(128, metadata!.Id);
    }
}
