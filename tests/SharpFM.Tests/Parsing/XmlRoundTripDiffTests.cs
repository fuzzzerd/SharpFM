using System.Xml.Linq;
using SharpFM.Model.Parsing;

namespace SharpFM.Tests.Parsing;

public class XmlRoundTripDiffTests
{
    [Fact]
    public void IdenticalTrees_ProduceNoDiagnostics()
    {
        var input = XElement.Parse("<root><child>x</child></root>");
        var output = XElement.Parse("<root><child>x</child></root>");

        Assert.Empty(XmlRoundTripDiff.Compute(input, output));
    }

    [Fact]
    public void AttributeOrderDoesNotMatter()
    {
        var input = XElement.Parse("<root><Step a=\"1\" b=\"2\"/></root>");
        var output = XElement.Parse("<root><Step b=\"2\" a=\"1\"/></root>");

        Assert.Empty(XmlRoundTripDiff.Compute(input, output));
    }

    [Fact]
    public void ChildOrderDoesNotMatter_WithinAParent()
    {
        var input = XElement.Parse("<root><a/><b/></root>");
        var output = XElement.Parse("<root><b/><a/></root>");

        Assert.Empty(XmlRoundTripDiff.Compute(input, output));
    }

    [Fact]
    public void WhitespaceOnlyDifferencesAreIgnored()
    {
        var input = XElement.Parse("<root><leaf>  hello  </leaf></root>");
        var output = XElement.Parse("<root><leaf>hello</leaf></root>");

        Assert.Empty(XmlRoundTripDiff.Compute(input, output));
    }

    [Fact]
    public void UnmodeledStepElement_ReportsUnknownStepElement()
    {
        var input = XElement.Parse("<fmxmlsnippet><Step><Mystery/></Step></fmxmlsnippet>");
        var output = XElement.Parse("<fmxmlsnippet><Step/></fmxmlsnippet>");

        var diags = XmlRoundTripDiff.Compute(input, output);

        var diag = Assert.Single(diags);
        Assert.Equal(ParseDiagnosticKind.UnknownStepElement, diag.Kind);
        Assert.Contains("Mystery", diag.Location);
    }

    [Fact]
    public void UnmodeledStepAttribute_ReportsUnknownStepAttribute()
    {
        var input = XElement.Parse("<fmxmlsnippet><Step mystery=\"x\"/></fmxmlsnippet>");
        var output = XElement.Parse("<fmxmlsnippet><Step/></fmxmlsnippet>");

        var diag = Assert.Single(XmlRoundTripDiff.Compute(input, output));
        Assert.Equal(ParseDiagnosticKind.UnknownStepAttribute, diag.Kind);
        Assert.Contains("@mystery", diag.Location);
    }

    [Fact]
    public void UnmodeledClipElement_ReportsUnknownClipElement()
    {
        var input = XElement.Parse("<fmxmlsnippet><Mystery/></fmxmlsnippet>");
        var output = XElement.Parse("<fmxmlsnippet/>");

        var diag = Assert.Single(XmlRoundTripDiff.Compute(input, output));
        Assert.Equal(ParseDiagnosticKind.UnknownClipElement, diag.Kind);
    }

    [Fact]
    public void UnmodeledClipAttribute_ReportsUnknownClipAttribute()
    {
        var input = XElement.Parse("<fmxmlsnippet mystery=\"x\"/>");
        var output = XElement.Parse("<fmxmlsnippet/>");

        var diag = Assert.Single(XmlRoundTripDiff.Compute(input, output));
        Assert.Equal(ParseDiagnosticKind.UnknownClipAttribute, diag.Kind);
    }

    [Fact]
    public void TextValueDifference_ReportsRoundTripValueMismatch()
    {
        var input = XElement.Parse("<root><leaf>original</leaf></root>");
        var output = XElement.Parse("<root><leaf>changed</leaf></root>");

        var diag = Assert.Single(XmlRoundTripDiff.Compute(input, output));
        Assert.Equal(ParseDiagnosticKind.RoundTripValueMismatch, diag.Kind);
    }

    [Fact]
    public void AttributeValueDifference_ReportsRoundTripValueMismatch()
    {
        var input = XElement.Parse("<root attr=\"a\"/>");
        var output = XElement.Parse("<root attr=\"b\"/>");

        var diag = Assert.Single(XmlRoundTripDiff.Compute(input, output));
        Assert.Equal(ParseDiagnosticKind.RoundTripValueMismatch, diag.Kind);
        Assert.Contains("@attr", diag.Location);
    }

    [Fact]
    public void OutputEmitsExtraElement_ReportsAsInfo()
    {
        var input = XElement.Parse("<root/>");
        var output = XElement.Parse("<root><DefaultedChild/></root>");

        var diag = Assert.Single(XmlRoundTripDiff.Compute(input, output));
        Assert.Equal(ParseDiagnosticSeverity.Info, diag.Severity);
        Assert.Contains("default", diag.Message);
    }

    [Fact]
    public void DroppedNamespace_ReportsDroppedNamespace()
    {
        var input = XElement.Parse("<root xmlns:x=\"urn:x\"><x:thing/></root>");
        var output = XElement.Parse("<root><thing/></root>");

        var diag = XmlRoundTripDiff.Compute(input, output)
            .Single(d => d.Kind == ParseDiagnosticKind.DroppedNamespace);
        Assert.Contains("urn:x", diag.Message);
    }

    [Fact]
    public void Location_UsesPositionalIndexForRepeatedNames()
    {
        var input = XElement.Parse("<root><Step><A/></Step><Step><B/></Step></root>");
        var output = XElement.Parse("<root><Step/><Step/></root>");

        var diags = XmlRoundTripDiff.Compute(input, output);
        Assert.Equal(2, diags.Count);
        Assert.Contains(diags, d => d.Location.Contains("Step[1]"));
        Assert.Contains(diags, d => d.Location.Contains("Step[2]"));
    }

    [Fact]
    public void NestedDifferences_AreReportedWithFullPath()
    {
        var input = XElement.Parse("<root><Step><Inner mystery=\"x\"/></Step></root>");
        var output = XElement.Parse("<root><Step><Inner/></Step></root>");

        var diag = Assert.Single(XmlRoundTripDiff.Compute(input, output));
        Assert.Contains("Step[1]", diag.Location);
        Assert.Contains("Inner[1]", diag.Location);
        Assert.Contains("@mystery", diag.Location);
    }
}
