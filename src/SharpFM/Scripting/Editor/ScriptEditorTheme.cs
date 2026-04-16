using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Media;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Shared color constants for script editor renderers.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ScriptEditorTheme
{
    internal static readonly IPen ErrorPen = new Pen(Brushes.Red, 1.0);
    internal static readonly IPen WarningPen = new Pen(Brushes.Gold, 1.0);
    internal static readonly IBrush BracketMatchBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
    internal static readonly IPen BracketMatchPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1.0);
    internal static readonly IBrush StatementHighlightBrush = new SolidColorBrush(Color.FromArgb(20, 100, 180, 255));
    internal static readonly IPen ContinuationRailPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 100, 180, 255)), 1.0);
    internal static readonly IPen SealedSquigglePen = new Pen(new SolidColorBrush(Color.FromArgb(220, 220, 170, 40)), 1.0);
}
