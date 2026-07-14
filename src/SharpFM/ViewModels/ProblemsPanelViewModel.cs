using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using SharpFM.Model.Parsing;

namespace SharpFM.ViewModels;

/// <summary>
/// One row in the Problems panel — a <see cref="ClipParseDiagnostic"/> plus
/// which axis of <see cref="ClipParseReport"/> it came from and the display
/// labels the panel needs.
/// </summary>
public sealed record ProblemRow(
    ParseDiagnosticKind Kind,
    ParseDiagnosticSeverity Severity,
    string Location,
    string Message,
    bool IsSemantic)
{
    public string SeverityGlyph => Severity.Glyph();

    public IBrush SeverityBrush => Severity.Brush();

    public string KindLabel => Kind.ToHumanLabel(1);
}

/// <summary>
/// Backs the bottom-docked Problems panel: the full list of parse diagnostics
/// for the selected clip (unlike the status bar's aggregate-only summary),
/// plus the raw XML at the currently selected diagnostic's location.
/// </summary>
public sealed class ProblemsPanelViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public ObservableCollection<ProblemRow> Diagnostics { get; } = [];

    public int Count => Diagnostics.Count;

    /// <summary>
    /// E.g. <c>"Problems (1 error, 2 info)"</c> — a severity breakdown rather
    /// than a flat count, so the count itself signals how alarmed to be.
    /// Ordered worst-first, relying on <see cref="ParseDiagnosticSeverity"/>'s
    /// declared order (see that enum's doc comment).
    /// </summary>
    public string HeaderText
    {
        get
        {
            if (Count == 0) return "Problems (0)";
            var breakdown = Diagnostics
                .GroupBy(r => r.Severity)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var count = g.Count();
                    return $"{count} {g.Key.Noun(count)}";
                });
            return $"Problems ({string.Join(", ", breakdown)})";
        }
    }

    private ProblemRow? _selectedDiagnostic;
    private string? _rawXml;

    public ProblemRow? SelectedDiagnostic
    {
        get => _selectedDiagnostic;
        set
        {
            if (Equals(_selectedDiagnostic, value)) return;
            _selectedDiagnostic = value;
            SelectedXmlSnippet = value is null || _rawXml is null
                ? null
                : ClipParseLocationResolver.Resolve(_rawXml, value.Location);
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(SelectedXmlSnippet));
        }
    }

    /// <summary>Raw XML resolved at <see cref="SelectedDiagnostic"/>'s location; cached alongside the selection.</summary>
    public string? SelectedXmlSnippet { get; private set; }

    private bool _isPanelVisible;

    public bool IsPanelVisible
    {
        get => _isPanelVisible;
        set
        {
            if (_isPanelVisible == value) return;
            _isPanelVisible = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Rebuild the diagnostic list for <paramref name="clip"/> (or clear it for
    /// null). A no-op, selection-preserving skip when the diagnostics haven't
    /// actually changed since the last refresh — the common case while typing,
    /// since most edits don't change the fidelity report.
    /// </summary>
    public void RefreshFrom(ClipViewModel? clip)
    {
        _rawXml = clip?.Clip.Xml;

        var rows = clip is null
            ? []
            : clip.ParseReport.Diagnostics.Select(d => ToRow(d, isSemantic: false))
                .Concat(clip.ParseReport.SemanticDiagnostics.Select(d => ToRow(d, isSemantic: true)))
                // Worst severity first, relying on ParseDiagnosticSeverity's
                // declared order (see that enum's doc comment); OrderBy is
                // stable so same-severity rows keep their original relative order.
                .OrderBy(r => r.Severity)
                .ToList();

        if (rows.SequenceEqual(Diagnostics))
        {
            return;
        }

        Diagnostics.Clear();
        foreach (var row in rows)
        {
            Diagnostics.Add(row);
        }

        SelectedDiagnostic = null;
        NotifyPropertyChanged(nameof(Count));
        NotifyPropertyChanged(nameof(HeaderText));
    }

    private static ProblemRow ToRow(ClipParseDiagnostic diag, bool isSemantic) =>
        new(diag.Kind, diag.Severity, diag.Location, diag.Message, isSemantic);
}
