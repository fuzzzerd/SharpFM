using SharpFM.Model.Parsing;

namespace SharpFM.Tests.Parsing;

public class ClipParseReportTests
{
    [Fact]
    public void Empty_HasZeroDiagnostics()
    {
        Assert.Empty(ClipParseReport.Empty.Diagnostics);
    }

    [Fact]
    public void Empty_IsLossless()
    {
        Assert.True(ClipParseReport.Empty.IsLossless);
    }

    [Fact]
    public void Empty_IsSingleton()
    {
        Assert.Same(ClipParseReport.Empty, ClipParseReport.Empty);
    }

    [Fact]
    public void Report_WithAnyDiagnostic_IsNotLossless()
    {
        var report = new ClipParseReport(
        [
            new ClipParseDiagnostic(
                ParseDiagnosticKind.UnknownStepElement,
                ParseDiagnosticSeverity.Warning,
                "/fmxmlsnippet/Step[1]/Mystery",
                "unmodeled child element"),
        ]);

        Assert.False(report.IsLossless);
        Assert.Single(report.Diagnostics);
    }

    [Fact]
    public void Report_PreservesDiagnosticOrder()
    {
        var first = new ClipParseDiagnostic(
            ParseDiagnosticKind.UnknownStep,
            ParseDiagnosticSeverity.Warning,
            "/fmxmlsnippet/Step[1]",
            "unknown step");
        var second = new ClipParseDiagnostic(
            ParseDiagnosticKind.UnknownStepAttribute,
            ParseDiagnosticSeverity.Info,
            "/fmxmlsnippet/Step[2]/@mystery",
            "unmodeled attribute");

        var report = new ClipParseReport([first, second]);

        Assert.Equal(first, report.Diagnostics[0]);
        Assert.Equal(second, report.Diagnostics[1]);
    }
}
