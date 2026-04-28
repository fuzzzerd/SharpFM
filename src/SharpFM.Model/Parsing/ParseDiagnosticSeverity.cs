namespace SharpFM.Model.Parsing;

/// <summary>
/// Severity of a <see cref="ClipParseDiagnostic"/>. Intentionally distinct from
/// <see cref="SharpFM.Model.Scripting.DiagnosticSeverity"/>: that type belongs to
/// the display-text validator (display→XML), this one to XML→domain parse fidelity.
/// </summary>
public enum ParseDiagnosticSeverity
{
    Error,
    Warning,
    Info,
}
