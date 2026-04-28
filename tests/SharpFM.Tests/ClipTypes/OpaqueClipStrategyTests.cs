using SharpFM.Model.ClipTypes;
using SharpFM.Model.Parsing;

namespace SharpFM.Tests.ClipTypes;

public class OpaqueClipStrategyTests
{
    [Fact]
    public void Parse_WellFormedXml_ReturnsSuccessWithLosslessReport()
    {
        var result = OpaqueClipStrategy.Instance.Parse("<root><child/></root>");

        var success = Assert.IsType<ParseSuccess>(result);
        Assert.IsType<OpaqueClipModel>(success.Model);
        Assert.True(success.Report.IsLossless);
    }

    [Fact]
    public void Parse_PreservesXmlVerbatim()
    {
        const string xml = "<root attr=\"value\"><child>text</child></root>";
        var result = OpaqueClipStrategy.Instance.Parse(xml);

        var success = Assert.IsType<ParseSuccess>(result);
        var model = Assert.IsType<OpaqueClipModel>(success.Model);
        Assert.Equal(xml, model.Xml);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsFailure()
    {
        var result = OpaqueClipStrategy.Instance.Parse("");

        var failure = Assert.IsType<ParseFailure>(result);
        Assert.Single(failure.Report.Diagnostics);
        Assert.Equal(ParseDiagnosticKind.XmlMalformed, failure.Report.Diagnostics[0].Kind);
    }

    [Fact]
    public void Parse_MalformedXml_ReturnsFailureWithDiagnostic()
    {
        var result = OpaqueClipStrategy.Instance.Parse("<unclosed>");

        var failure = Assert.IsType<ParseFailure>(result);
        Assert.Equal(ParseDiagnosticKind.XmlMalformed, failure.Report.Diagnostics[0].Kind);
        Assert.Equal(ParseDiagnosticSeverity.Error, failure.Report.Diagnostics[0].Severity);
    }

    [Fact]
    public void DefaultXml_ProducesParseableSnippet()
    {
        var seed = OpaqueClipStrategy.Instance.DefaultXml("anything");
        var result = OpaqueClipStrategy.Instance.Parse(seed);
        Assert.IsType<ParseSuccess>(result);
    }
}
