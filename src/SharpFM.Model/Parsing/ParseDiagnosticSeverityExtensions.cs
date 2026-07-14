namespace SharpFM.Model.Parsing;

/// <summary>
/// Human-readable labels for <see cref="ParseDiagnosticSeverity"/>. Lives next
/// to the enum so any consumer surfacing a severity breakdown (Problems panel
/// header, future MCP tools) shares the same wording. Mirrors
/// <see cref="ParseDiagnosticKindExtensions"/>'s convention for the sibling
/// <see cref="ParseDiagnosticKind"/> enum.
/// </summary>
public static class ParseDiagnosticSeverityExtensions
{
    /// <summary>
    /// Format <paramref name="severity"/> as a noun phrase, choosing the
    /// singular form when <paramref name="count"/> is 1 and the plural
    /// otherwise. <c>Info</c> doesn't pluralize.
    /// </summary>
    public static string Noun(this ParseDiagnosticSeverity severity, int count) => severity switch
    {
        ParseDiagnosticSeverity.Error => count == 1 ? "error" : "errors",
        ParseDiagnosticSeverity.Warning => count == 1 ? "warning" : "warnings",
        _ => "info",
    };
}
