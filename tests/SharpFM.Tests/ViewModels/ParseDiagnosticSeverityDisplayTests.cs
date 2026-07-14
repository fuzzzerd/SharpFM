using SharpFM.Model.Parsing;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class ParseDiagnosticSeverityDisplayTests
{
    [Theory]
    [InlineData(ParseDiagnosticSeverity.Error, "✕")]
    [InlineData(ParseDiagnosticSeverity.Warning, "!")]
    [InlineData(ParseDiagnosticSeverity.Info, "i")]
    public void Glyph_MapsEachSeverity(ParseDiagnosticSeverity severity, string expected)
    {
        Assert.Equal(expected, severity.Glyph());
    }

    [Fact]
    public void Brush_MapsErrorToRed()
    {
        Assert.Equal(Avalonia.Media.Brushes.IndianRed, ParseDiagnosticSeverity.Error.Brush());
    }

    [Fact]
    public void Brush_MapsWarningToDarkOrange()
    {
        Assert.Equal(Avalonia.Media.Brushes.DarkOrange, ParseDiagnosticSeverity.Warning.Brush());
    }

    [Fact]
    public void Brush_MapsInfoToGray()
    {
        Assert.Equal(Avalonia.Media.Brushes.Gray, ParseDiagnosticSeverity.Info.Brush());
    }
}
