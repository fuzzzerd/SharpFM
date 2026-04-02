using Avalonia;
using Avalonia.Media;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Shared color constants for script editor renderers.
/// </summary>
internal static class ScriptEditorTheme
{
    internal static readonly IPen ErrorPen = new Pen(Brushes.Red, 1.0);
    internal static readonly IPen WarningPen = new Pen(Brushes.Gold, 1.0);
    internal static readonly IBrush BracketMatchBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
    internal static readonly IPen BracketMatchPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1.0);
    internal static readonly IBrush StatementHighlightBrush = new SolidColorBrush(Color.FromArgb(20, 100, 180, 255));
}
