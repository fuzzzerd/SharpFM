using SharpFM.Model.ClipTypes;
using SharpFM.Model.Parsing;

namespace SharpFM.Tests.ClipTypes;

public class LayoutClipStrategyTests
{
    [Fact]
    public void Identity_IsLayout()
    {
        Assert.Equal("Mac-XML2", LayoutClipStrategy.Instance.FormatId);
        Assert.Equal("Layout", LayoutClipStrategy.Instance.DisplayName);
    }

    [Fact]
    public void Parse_ValidLayoutSnippet_ReturnsSuccess()
    {
        var result = LayoutClipStrategy.Instance.Parse(
            "<fmxmlsnippet type=\"FMObjectList\"><Layout/></fmxmlsnippet>");

        var success = Assert.IsType<ParseSuccess>(result);
        Assert.IsType<LayoutClipModel>(success.Model);
        Assert.True(success.Report.IsLossless);
    }

    [Fact]
    public void Parse_PreservesLayoutXmlVerbatim()
    {
        const string xml = "<fmxmlsnippet type=\"FMObjectList\"><Layout name=\"Main\"/></fmxmlsnippet>";
        var result = LayoutClipStrategy.Instance.Parse(xml);

        var success = Assert.IsType<ParseSuccess>(result);
        var model = Assert.IsType<LayoutClipModel>(success.Model);
        Assert.Equal(xml, model.Xml);
    }

    [Fact]
    public void Parse_MalformedXml_ReturnsFailure()
    {
        var result = LayoutClipStrategy.Instance.Parse("<broken");
        Assert.IsType<ParseFailure>(result);
    }

    [Fact]
    public void Parse_WrongRoot_ReturnsUnsupportedClipType()
    {
        var result = LayoutClipStrategy.Instance.Parse("<wrong/>");
        var failure = Assert.IsType<ParseFailure>(result);
        Assert.Equal(ParseDiagnosticKind.UnsupportedClipType, failure.Report.Diagnostics[0].Kind);
    }
}
