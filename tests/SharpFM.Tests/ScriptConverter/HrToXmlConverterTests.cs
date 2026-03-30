using System.Xml.Linq;
using SharpFM.Core.ScriptConverter;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class HrToXmlConverterTests
{
    [Fact]
    public void ConvertsComment()
    {
        var result = HrToXmlConverter.Convert("# hello");
        var doc = XDocument.Parse(result.Xml);
        var step = doc.Root!.Element("Step")!;
        Assert.Equal("89", step.Attribute("id")?.Value);
        Assert.Equal("# (comment)", step.Attribute("name")?.Value);
        Assert.Equal("hello", step.Element("Text")?.Value);
    }

    [Fact]
    public void ConvertsSetVariable()
    {
        var result = HrToXmlConverter.Convert("Set Variable [ $count ; Value: $count + 1 ]");
        var doc = XDocument.Parse(result.Xml);
        var step = doc.Root!.Element("Step")!;
        Assert.Equal("141", step.Attribute("id")?.Value);
        Assert.Equal("$count", step.Element("Name")?.Value);
        Assert.Equal("$count + 1", step.Element("Value")?.Element("Calculation")?.Value);
    }

    [Fact]
    public void ConvertsIfEndIf()
    {
        var result = HrToXmlConverter.Convert("If [ $x > 0 ]\nEnd If");
        var doc = XDocument.Parse(result.Xml);
        var steps = doc.Root!.Elements("Step").ToArray();
        Assert.Equal(2, steps.Length);
        Assert.Equal("68", steps[0].Attribute("id")?.Value);
        Assert.Equal("$x > 0", steps[0].Element("Calculation")?.Value);
        Assert.Equal("70", steps[1].Attribute("id")?.Value);
    }

    [Fact]
    public void ConvertsDisabledStep()
    {
        var result = HrToXmlConverter.Convert("// Beep");
        var doc = XDocument.Parse(result.Xml);
        var step = doc.Root!.Element("Step")!;
        Assert.Equal("False", step.Attribute("enable")?.Value);
    }

    [Fact]
    public void ConvertsEmptyInput()
    {
        var result = HrToXmlConverter.Convert("");
        Assert.NotNull(result.Xml);
        var doc = XDocument.Parse(result.Xml);
        Assert.Equal("fmxmlsnippet", doc.Root!.Name.LocalName);
    }

    [Fact]
    public void ConvertsUnknownStep()
    {
        var result = HrToXmlConverter.Convert("SomeNewStep [ param1 ]");
        var doc = XDocument.Parse(result.Xml);
        var step = doc.Root!.Element("Step")!;
        // Unknown steps become comments
        Assert.Equal("89", step.Attribute("id")?.Value);
        Assert.Contains("SomeNewStep", step.Element("Text")?.Value);
    }

    [Fact]
    public void ConvertsSelfClosingStep()
    {
        var result = HrToXmlConverter.Convert("End Loop");
        var doc = XDocument.Parse(result.Xml);
        var step = doc.Root!.Element("Step")!;
        Assert.Equal("73", step.Attribute("id")?.Value);
        Assert.Equal("End Loop", step.Attribute("name")?.Value);
    }

    [Fact]
    public void ConvertsSetField()
    {
        var result = HrToXmlConverter.Convert("Set Field [ Invoices::Status ; \"Done\" ]");
        var doc = XDocument.Parse(result.Xml);
        var step = doc.Root!.Element("Step")!;
        Assert.Equal("76", step.Attribute("id")?.Value);
        var field = step.Element("Field")!;
        Assert.Equal("Invoices", field.Attribute("table")?.Value);
        Assert.Equal("Status", field.Attribute("name")?.Value);
        Assert.Equal("\"Done\"", step.Element("Calculation")?.Value);
    }

    [Fact]
    public void OutputIsValidXml()
    {
        var scripts = new[]
        {
            "# comment",
            "Set Variable [ $x ; Value: 1 ]",
            "If [ $x > 0 ]\n    Beep\nEnd If",
            "Set Field [ T::F ; \"val\" ]",
            "// Beep",
            "End Loop",
            "Loop\n    Exit Loop If [ $done ]\nEnd Loop"
        };

        foreach (var script in scripts)
        {
            var result = HrToXmlConverter.Convert(script);
            Assert.Empty(result.Errors);
            // Should not throw
            XDocument.Parse(result.Xml);
        }
    }

    [Fact]
    public void ConvertsMultipleSteps()
    {
        var input = "# Start\nSet Variable [ $x ; Value: 1 ]\nBeep";
        var result = HrToXmlConverter.Convert(input);
        var doc = XDocument.Parse(result.Xml);
        var steps = doc.Root!.Elements("Step").ToArray();
        Assert.Equal(3, steps.Length);
    }

    [Fact]
    public void BareTextAfterComment_MergesIntoComment()
    {
        var input = "# this is my comment\nmore text";
        var result = HrToXmlConverter.Convert(input);
        var doc = XDocument.Parse(result.Xml);
        var steps = doc.Root!.Elements("Step").ToArray();
        // Should produce a single comment, not two steps
        Assert.Single(steps);
        Assert.Equal("89", steps[0].Attribute("id")?.Value);
        Assert.Contains("this is my comment", steps[0].Element("Text")?.Value);
        Assert.Contains("more text", steps[0].Element("Text")?.Value);
    }

    [Fact]
    public void BareTextNotAfterComment_BecomesNewComment()
    {
        var input = "Set Variable [ $x ; Value: 1 ]\nsome random text";
        var result = HrToXmlConverter.Convert(input);
        var doc = XDocument.Parse(result.Xml);
        var steps = doc.Root!.Elements("Step").ToArray();
        Assert.Equal(2, steps.Length);
        // Second step is a comment (not [Unknown])
        Assert.Equal("89", steps[1].Attribute("id")?.Value);
        Assert.Equal("some random text", steps[1].Element("Text")?.Value);
        Assert.DoesNotContain("[Unknown]", steps[1].Element("Text")?.Value ?? "");
    }

    [Fact]
    public void MultipleBareTextLinesAfterComment_AllMerge()
    {
        var input = "# line one\nline two\nline three";
        var result = HrToXmlConverter.Convert(input);
        var doc = XDocument.Parse(result.Xml);
        var steps = doc.Root!.Elements("Step").ToArray();
        Assert.Single(steps);
        var text = steps[0].Element("Text")?.Value;
        Assert.Contains("line one", text);
        Assert.Contains("line two", text);
        Assert.Contains("line three", text);
    }
}
