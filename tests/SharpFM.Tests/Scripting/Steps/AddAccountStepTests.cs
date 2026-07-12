using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Tests.Scripting.Steps;

public class AddAccountStepTests
{
    // Canonical (skill): ChgPwdOnNextLogin first, then the <AddAccount> wrapper
    // with a text <AccountType> (was a flat, value-attr, wrapper-less form).
    private const string CanonicalXml = """<Step enable="True" id="134" name="Add Account"><ChgPwdOnNextLogin value="True"/><AddAccount><AccountType>FileMaker</AccountType><AccountName><Calculation><![CDATA[$x]]></Calculation></AccountName><Password><Calculation><![CDATA[$x]]></Calculation></Password><PrivilegeSet>$example</PrivilegeSet></AddAccount></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = AddAccountStep.Parse(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_RoundTripsThroughFromDisplayParams()
    {
        var step1 = AddAccountStep.Parse(XElement.Parse(CanonicalXml));
        var display = step1.ToDisplayLine();
        var open = display.IndexOf('[');
        var close = display.LastIndexOf(']');
        var inner = display.Substring(open + 1, close - open - 1).Trim();
        var tokens = inner.Split(';', System.StringSplitOptions.TrimEntries);

        var step2 = StepDisplayFactory.TryCreate(AddAccountStep.XmlName, true, tokens)!;
        Assert.True(XNode.DeepEquals(step1.ToXml(), step2.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Add Account", out var metadata));
        Assert.Equal(134, metadata!.Id);
    }
}
