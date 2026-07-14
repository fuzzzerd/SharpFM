namespace SharpFM.Model.Parsing;

/// <summary>
/// Severity of a <see cref="ClipParseDiagnostic"/>. Intentionally distinct from
/// <see cref="SharpFM.Model.Scripting.DiagnosticSeverity"/>: that type belongs to
/// the display-text validator (display→XML), this one to XML→domain parse fidelity.
/// </summary>
/// <remarks>
/// Declared worst-to-least-severe on purpose: <see cref="ClipParseReport.HighestSeverity"/>
/// and the Problems panel's sort/breakdown (<c>ProblemsPanelViewModel</c>) both
/// rely on the ordinal — the numerically smallest value wins. Inserting a new
/// value out of severity order, or reordering these, will silently change both.
/// </remarks>
public enum ParseDiagnosticSeverity
{
    Error,
    Warning,
    Info,
}
