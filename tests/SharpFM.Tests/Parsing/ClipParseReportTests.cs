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

    [Fact]
    public void SemanticDiagnostics_DefaultsToEmpty()
    {
        var report = new ClipParseReport([]);

        Assert.Empty(report.SemanticDiagnostics);
    }

    [Fact]
    public void Empty_SemanticDiagnostics_IsEmpty()
    {
        Assert.Empty(ClipParseReport.Empty.SemanticDiagnostics);
    }

    [Fact]
    public void IsSemanticallyValid_TrueWhenSemanticDiagnosticsEmpty()
    {
        Assert.True(ClipParseReport.Empty.IsSemanticallyValid);
    }

    [Fact]
    public void IsSemanticallyValid_FalseWhenAnySemanticDiagnostic()
    {
        var report = new ClipParseReport([])
        {
            SemanticDiagnostics =
            [
                new ClipParseDiagnostic(
                    ParseDiagnosticKind.UnknownStep,
                    ParseDiagnosticSeverity.Warning,
                    "/fmxmlsnippet/Step[1]/Name",
                    "variable name missing $ prefix"),
            ],
        };

        Assert.False(report.IsSemanticallyValid);
        Assert.True(report.IsLossless);
    }

    [Fact]
    public void HighestSeverity_NullWhenNoDiagnostics()
    {
        Assert.Null(ClipParseReport.Empty.HighestSeverity);
    }

    [Fact]
    public void HighestSeverity_InfoOnly_IsInfo()
    {
        var report = new ClipParseReport(
        [
            new ClipParseDiagnostic(
                ParseDiagnosticKind.RoundTripValueMismatch,
                ParseDiagnosticSeverity.Info,
                "/fmxmlsnippet/Step[1]/Restore",
                "output emitted a default"),
        ]);

        Assert.Equal(ParseDiagnosticSeverity.Info, report.HighestSeverity);
    }

    [Fact]
    public void HighestSeverity_MixedInfoAndWarning_IsWarning()
    {
        var report = new ClipParseReport(
        [
            new ClipParseDiagnostic(
                ParseDiagnosticKind.RoundTripValueMismatch,
                ParseDiagnosticSeverity.Info,
                "/fmxmlsnippet/Step[1]/Restore",
                "output emitted a default"),
            new ClipParseDiagnostic(
                ParseDiagnosticKind.UnknownStepElement,
                ParseDiagnosticSeverity.Warning,
                "/fmxmlsnippet/Step[2]/Mystery",
                "unmodeled child element"),
        ]);

        Assert.Equal(ParseDiagnosticSeverity.Warning, report.HighestSeverity);
    }

    [Fact]
    public void HighestSeverity_WithError_IsError()
    {
        var report = new ClipParseReport(
        [
            new ClipParseDiagnostic(
                ParseDiagnosticKind.RoundTripValueMismatch,
                ParseDiagnosticSeverity.Info,
                "/fmxmlsnippet/Step[1]/Restore",
                "output emitted a default"),
            new ClipParseDiagnostic(
                ParseDiagnosticKind.XmlMalformed,
                ParseDiagnosticSeverity.Error,
                "/",
                "not well-formed"),
        ]);

        Assert.Equal(ParseDiagnosticSeverity.Error, report.HighestSeverity);
    }

    [Fact]
    public void HighestSeverity_ConsidersSemanticDiagnosticsToo()
    {
        var report = new ClipParseReport([])
        {
            SemanticDiagnostics =
            [
                new ClipParseDiagnostic(
                    ParseDiagnosticKind.UnknownStep,
                    ParseDiagnosticSeverity.Info,
                    "/fmxmlsnippet/Step[1]/Name",
                    "informational note"),
            ],
        };

        Assert.Equal(ParseDiagnosticSeverity.Info, report.HighestSeverity);
    }
}
