using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class RevertTransactionStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="207" name="Revert Transaction"><Option state="True"/><Condition><Calculation><![CDATA[$x]]></Calculation></Condition><ErrorCode><Calculation><![CDATA[$x]]></Calculation></ErrorCode><ErrorMessage><Calculation><![CDATA[$x]]></Calculation></ErrorMessage></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = RevertTransactionStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Revert Transaction", out var metadata));
        Assert.Equal(207, metadata!.Id);
    }
}
