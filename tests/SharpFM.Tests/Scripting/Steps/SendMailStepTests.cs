using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class SendMailStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="63" name="Send Mail"><NoInteract state="True" /><Attachment><UniversalPathList>$attachmentFilePathList</UniversalPathList></Attachment><To UseFoundSet="False"><Calculation><![CDATA[$to]]></Calculation></To><Cc UseFoundSet="False"><Calculation><![CDATA[$cc]]></Calculation></Cc><Bcc UseFoundSet="False"><Calculation><![CDATA[$bcc]]></Calculation></Bcc><Subject><Calculation><![CDATA[$subject]]></Calculation></Subject><Message><Calculation><![CDATA[$message]]></Calculation></Message></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SendMailStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void HotFields_ReadThroughBag()
    {
        var step = (SendMailStep)SendMailStep.Metadata.FromXml!(XElement.Parse(CanonicalXml));
        Assert.Equal("$to", step.To!.Text);
        Assert.Equal("$subject", step.Subject!.Text);
        Assert.Equal("$message", step.Message!.Text);
    }

    [Fact]
    public void RoundTrip_PreservesUseFoundSetAttribute()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = SendMailStep.Metadata.FromXml!(source);
        var output = step.ToXml();
        Assert.Equal("False", output.Element("To")!.Attribute("UseFoundSet")!.Value);
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Send Mail", out var metadata));
        Assert.Equal(63, metadata!.Id);
    }
}
