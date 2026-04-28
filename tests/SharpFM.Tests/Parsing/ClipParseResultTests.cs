using SharpFM.Model.Parsing;
using SharpFM.Model.Scripting;

namespace SharpFM.Tests.Parsing;

public class ClipParseResultTests
{
    [Fact]
    public void ParseSuccess_CarriesModelAndReport()
    {
        var script = new FmScript([]);
        var model = new ScriptClipModel(script);
        var result = new ParseSuccess(model, ClipParseReport.Empty);

        Assert.Same(script, ((ScriptClipModel)result.Model).Script);
        Assert.True(result.Report.IsLossless);
    }

    [Fact]
    public void ParseFailure_CarriesReasonAndReport()
    {
        var report = new ClipParseReport(
        [
            new ClipParseDiagnostic(
                ParseDiagnosticKind.XmlMalformed,
                ParseDiagnosticSeverity.Error,
                "/",
                "unexpected end of stream"),
        ]);

        var result = new ParseFailure("invalid xml", report);

        Assert.Equal("invalid xml", result.Reason);
        Assert.False(result.Report.IsLossless);
    }

    [Fact]
    public void Result_PatternMatchesByKind()
    {
        ClipParseResult success = new ParseSuccess(
            new OpaqueClipModel("<x/>"),
            ClipParseReport.Empty);
        ClipParseResult failure = new ParseFailure("oops", ClipParseReport.Empty);

        Assert.True(success is ParseSuccess);
        Assert.True(failure is ParseFailure);
        Assert.False(success is ParseFailure);
        Assert.False(failure is ParseSuccess);
    }

    [Fact]
    public void ClipModel_VariantsAreDistinctTypes()
    {
        ClipModel script = new ScriptClipModel(new FmScript([]));
        ClipModel layout = new LayoutClipModel("<x/>");
        ClipModel opaque = new OpaqueClipModel("<x/>");

        Assert.IsType<ScriptClipModel>(script);
        Assert.IsType<LayoutClipModel>(layout);
        Assert.IsType<OpaqueClipModel>(opaque);
    }
}
