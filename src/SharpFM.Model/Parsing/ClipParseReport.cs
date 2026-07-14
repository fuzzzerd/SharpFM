using System.Collections.Generic;
using System.Linq;

namespace SharpFM.Model.Parsing;

/// <summary>
/// Aggregate of every <see cref="ClipParseDiagnostic"/> produced by parsing one
/// clip. Structural <see cref="Diagnostics"/> describe round-trip fidelity loss
/// detected by <see cref="XmlRoundTripDiff"/>; <see cref="SemanticDiagnostics"/>
/// describe domain-rule violations emitted by validators registered in
/// <see cref="Validation.SemanticValidatorRegistry"/>. The two axes are kept
/// distinct so consumers (status bar, tree glyph, plugins) can render structural
/// fidelity and semantic correctness independently.
/// </summary>
public sealed record ClipParseReport(IReadOnlyList<ClipParseDiagnostic> Diagnostics)
{
    /// <summary>Shared empty report used by clean parses to avoid allocating.</summary>
    public static ClipParseReport Empty { get; } = new([]);

    /// <summary>
    /// Domain-rule violations from semantic validators. Defaults to empty so
    /// the existing positional constructor stays source-compatible.
    /// </summary>
    public IReadOnlyList<ClipParseDiagnostic> SemanticDiagnostics { get; init; } = [];

    /// <summary>True if the parse produced no structural diagnostics.</summary>
    public bool IsLossless => Diagnostics.Count == 0;

    /// <summary>True if no semantic validators flagged the parsed model.</summary>
    public bool IsSemanticallyValid => SemanticDiagnostics.Count == 0;

    /// <summary>
    /// Most severe diagnostic across both <see cref="Diagnostics"/> and
    /// <see cref="SemanticDiagnostics"/>, or null when there are none. Relies
    /// on <see cref="ParseDiagnosticSeverity"/>'s declared order (see that
    /// enum's doc comment) so the numerically smallest value is the worst;
    /// <see cref="Enumerable.Min{T}(IEnumerable{T})"/> on a nullable sequence
    /// returns null for an empty sequence, so no separate empty-check is needed.
    /// </summary>
    public ParseDiagnosticSeverity? HighestSeverity =>
        Diagnostics.Concat(SemanticDiagnostics).Select(d => (ParseDiagnosticSeverity?)d.Severity).Min();
}
