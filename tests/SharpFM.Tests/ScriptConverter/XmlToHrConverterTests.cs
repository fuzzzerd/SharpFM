using SharpFM.Core.ScriptConverter;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class XmlToHrConverterTests
{
    private static string Wrap(string steps) =>
        $"<fmxmlsnippet type=\"FMObjectList\">{steps}</fmxmlsnippet>";

    [Fact]
    public void ConvertsComment()
    {
        var xml = Wrap("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>hello</Text></Step>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Equal("# hello", hr);
    }

    [Fact]
    public void ConvertsSetVariable()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[$count + 1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$count</Name>"
            + "</Step>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Equal("Set Variable [ $count ; Value: $count + 1 ]", hr);
    }

    [Fact]
    public void ConvertsSetVariableWithRepetition()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[\"x\"]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[2]]></Calculation></Repetition>"
            + "<Name>$arr</Name>"
            + "</Step>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Equal("Set Variable [ $arr[2] ; Value: \"x\" ]", hr);
    }

    [Fact]
    public void ConvertsIfEndIf()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"68\" name=\"If\"><Calculation><![CDATA[$x > 0]]></Calculation></Step>"
            + "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$y</Name></Step>"
            + "<Step enable=\"True\" id=\"70\" name=\"End If\"/>");
        var hr = XmlToHrConverter.Convert(xml);
        var lines = hr.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("If [ $x > 0 ]", lines[0]);
        Assert.Equal("    Set Variable [ $y ; Value: 1 ]", lines[1]);
        Assert.Equal("End If", lines[2]);
    }

    [Fact]
    public void ConvertsIfElseEndIf()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"68\" name=\"If\"><Calculation><![CDATA[$x > 0]]></Calculation></Step>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>positive</Text></Step>"
            + "<Step enable=\"True\" id=\"69\" name=\"Else\"/>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>negative</Text></Step>"
            + "<Step enable=\"True\" id=\"70\" name=\"End If\"/>");
        var hr = XmlToHrConverter.Convert(xml);
        var lines = hr.Split('\n');
        Assert.Equal(5, lines.Length);
        Assert.Equal("If [ $x > 0 ]", lines[0]);
        Assert.Equal("    # positive", lines[1]);
        Assert.Equal("Else", lines[2]);
        Assert.Equal("    # negative", lines[3]);
        Assert.Equal("End If", lines[4]);
    }

    [Fact]
    public void ConvertsNestedIfLoop()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"68\" name=\"If\"><Calculation><![CDATA[$x > 0]]></Calculation></Step>"
            + "<Step enable=\"True\" id=\"71\" name=\"Loop\"/>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>inner</Text></Step>"
            + "<Step enable=\"True\" id=\"73\" name=\"End Loop\"/>"
            + "<Step enable=\"True\" id=\"70\" name=\"End If\"/>");
        var hr = XmlToHrConverter.Convert(xml);
        var lines = hr.Split('\n');
        Assert.Equal("If [ $x > 0 ]", lines[0]);
        Assert.Equal("    Loop", lines[1]);
        Assert.Equal("        # inner", lines[2]);
        Assert.Equal("    End Loop", lines[3]);
        Assert.Equal("End If", lines[4]);
    }

    [Fact]
    public void ConvertsDisabledStep()
    {
        var xml = Wrap("<Step enable=\"False\" id=\"93\" name=\"Beep\"/>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Equal("// Beep", hr);
    }

    [Fact]
    public void ConvertsSetField()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Calculation><![CDATA[\"Done\"]]></Calculation>"
            + "<Field table=\"Invoices\" id=\"3\" name=\"Status\"/>"
            + "</Step>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Equal("Set Field [ Invoices::Status ; \"Done\" ]", hr);
    }

    [Fact]
    public void ConvertsSelfClosingStep()
    {
        var xml = Wrap("<Step enable=\"True\" id=\"7\" name=\"New Record/Request\"/>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Equal("New Record/Request", hr);
    }

    [Fact]
    public void ConvertsEmptyScript()
    {
        var xml = Wrap("");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Equal("", hr);
    }

    [Fact]
    public void ConvertsScriptWrapper()
    {
        var xml = "<fmxmlsnippet type=\"FMObjectList\">"
            + "<Script id=\"1\" name=\"Test\">"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>inside script</Text></Step>"
            + "</Script></fmxmlsnippet>";
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Equal("# inside script", hr);
    }

    [Fact]
    public void HandlesUnknownStep()
    {
        var xml = Wrap("<Step enable=\"True\" id=\"9999\" name=\"FutureStep\"><Foo>bar</Foo></Step>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Contains("FutureStep", hr);
    }

    [Fact]
    public void HandlesNullInput()
    {
        var hr = XmlToHrConverter.Convert("");
        Assert.Equal("", hr);
    }
}
