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

    /// <summary>
    /// Foreground brush applied to sealed-step text. Same hue as the
    /// default editor text but at reduced alpha — reads as "muted /
    /// not first-class" without flipping typeface (which would force
    /// AvaloniaEdit to re-shape the run on every layout).
    /// </summary>
    internal static readonly IBrush SealedTextBrush = new SolidColorBrush(Color.FromArgb(150, 200, 200, 200));

    /// <summary>
    /// Brush filling the thin accent stripe drawn at the left edge of
    /// every sealed-step line. Same gold family as the squiggle so the
    /// two cues read as one feature.
    /// </summary>
    internal static readonly IBrush SealedLeftStripeBrush = new SolidColorBrush(Color.FromArgb(180, 220, 170, 40));

    /// <summary>Width of the left-edge sealed-step accent stripe.</summary>
    internal const double SealedLeftStripeWidth = 3.0;
}
