using Avalonia.Media;
using SharpFM.Model.Parsing;

namespace SharpFM.ViewModels;

/// <summary>
/// Shared mapping from <see cref="ParseDiagnosticSeverity"/> to how it
/// renders in the UI — every place a severity needs a glyph or color
/// (Problems panel rows, the status bar, the clip-tree badge) goes through
/// this instead of re-switching on the enum. Lives here rather than next to
/// the enum in <c>SharpFM.Model</c> because <see cref="IBrush"/> requires the
/// Avalonia reference that project must not carry; the count-driven noun
/// phrase has no such dependency and lives on
/// <see cref="ParseDiagnosticSeverityExtensions"/> instead.
/// </summary>
public static class ParseDiagnosticSeverityDisplay
{
    public static string Glyph(this ParseDiagnosticSeverity severity) => severity switch
    {
        ParseDiagnosticSeverity.Error => "✕",
        ParseDiagnosticSeverity.Warning => "!",
        _ => "i",
    };

    public static IBrush Brush(this ParseDiagnosticSeverity severity) => severity switch
    {
        ParseDiagnosticSeverity.Error => Brushes.IndianRed,
        ParseDiagnosticSeverity.Warning => Brushes.DarkOrange,
        _ => Brushes.Gray,
    };
}
