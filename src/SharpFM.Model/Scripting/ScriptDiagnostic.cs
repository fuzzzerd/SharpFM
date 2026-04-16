namespace SharpFM.Model.Scripting;

public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info
}

public record ScriptDiagnostic(
    int Line,
    int StartCol,
    int EndCol,
    string Message,
    DiagnosticSeverity Severity
);
