using SharpFM.Model.Parsing;

namespace SharpFM.Tests.Parsing;

public class ParseDiagnosticSeverityExtensionsTests
{
    [Theory]
    [InlineData(ParseDiagnosticSeverity.Error, 1, "error")]
    [InlineData(ParseDiagnosticSeverity.Error, 2, "errors")]
    [InlineData(ParseDiagnosticSeverity.Warning, 1, "warning")]
    [InlineData(ParseDiagnosticSeverity.Warning, 2, "warnings")]
    [InlineData(ParseDiagnosticSeverity.Info, 1, "info")]
    [InlineData(ParseDiagnosticSeverity.Info, 2, "info")]
    public void Noun_PluralizesExceptInfo(ParseDiagnosticSeverity severity, int count, string expected)
    {
        Assert.Equal(expected, severity.Noun(count));
    }
}
