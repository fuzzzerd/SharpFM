using System.Collections.Generic;

namespace SharpFM.Model.Parsing;

/// <summary>
/// Aggregate of every <see cref="ClipParseDiagnostic"/> produced by parsing one
/// clip. Lossless when empty; consumers check <see cref="IsLossless"/> rather
/// than inspecting the collection directly.
/// </summary>
public sealed record ClipParseReport(IReadOnlyList<ClipParseDiagnostic> Diagnostics)
{
    /// <summary>Shared empty report used by clean parses to avoid allocating.</summary>
    public static ClipParseReport Empty { get; } = new([]);

    /// <summary>True if the parse produced no diagnostics of any kind.</summary>
    public bool IsLossless => Diagnostics.Count == 0;
}
