using System.Collections.Generic;

namespace SharpFM.Model.Parsing;

/// <summary>
/// Human-readable labels for <see cref="ParseDiagnosticKind"/>. Lives next to
/// the enum so any consumer surfacing parse diagnostics (UI, MCP tools,
/// future plugin API) shares the same wording.
/// </summary>
public static class ParseDiagnosticKindExtensions
{
    private static readonly Dictionary<ParseDiagnosticKind, (string Singular, string Plural)> Labels = new()
    {
        [ParseDiagnosticKind.UnknownStep] = ("unknown step", "unknown steps"),
        [ParseDiagnosticKind.UnknownStepElement] = ("unknown step element", "unknown step elements"),
        [ParseDiagnosticKind.UnknownStepAttribute] = ("unknown step attribute", "unknown step attributes"),
        [ParseDiagnosticKind.UnknownClipElement] = ("unknown element", "unknown elements"),
        [ParseDiagnosticKind.UnknownClipAttribute] = ("unknown attribute", "unknown attributes"),
        [ParseDiagnosticKind.DroppedNamespace] = ("dropped namespace", "dropped namespaces"),
        [ParseDiagnosticKind.RoundTripValueMismatch] = ("value mismatch", "value mismatches"),
        [ParseDiagnosticKind.XmlMalformed] = ("malformed xml", "malformed xml"),
        [ParseDiagnosticKind.UnsupportedClipType] = ("unsupported clip type", "unsupported clip type"),
    };

    /// <summary>
    /// Format <paramref name="kind"/> as a human-readable noun phrase, choosing
    /// the singular form when <paramref name="count"/> is 1 and the plural
    /// otherwise. Falls back to <c>"issue"</c> for unmapped kinds.
    /// </summary>
    public static string ToHumanLabel(this ParseDiagnosticKind kind, int count) =>
        Labels.TryGetValue(kind, out var pair)
            ? (count == 1 ? pair.Singular : pair.Plural)
            : "issue";
}
