using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ConfigureLocalNotificationStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="187" name="Configure Local Notification"><Action value="Schedule" /><Identifier><Calculation><![CDATA["notification_id"]]></Calculation></Identifier><Title><Calculation><![CDATA["title"]]></Calculation></Title><Body><Calculation><![CDATA["body"]]></Calculation></Body></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = ConfigureLocalNotificationStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Configure Local Notification", out var metadata));
        Assert.Equal(187, metadata!.Id);
    }
}
