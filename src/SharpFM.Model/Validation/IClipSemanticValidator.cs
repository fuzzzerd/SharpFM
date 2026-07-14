using System.Collections.Generic;
using SharpFM.Model.Parsing;

namespace SharpFM.Model.Validation;

/// <summary>
/// Inspects a parsed <see cref="ClipModel"/> and emits diagnostics for
/// domain-rule violations that <see cref="XmlRoundTripDiff"/> cannot detect —
/// e.g. FileMaker variable-name conventions or calculation-syntax errors.
/// Validators are pure, must not throw, and run on every successful parse.
/// </summary>
public interface IClipSemanticValidator
{
    /// <summary>
    /// Sentinel <see cref="FormatIds"/> entry that opts the validator into
    /// every clip type.
    /// </summary>
    public const string AllFormats = "*";

    /// <summary>
    /// Format ids this validator applies to (e.g. <c>"Mac-XMSS"</c>). Use
    /// <see cref="AllFormats"/> to opt into every clip type.
    /// </summary>
    IReadOnlyCollection<string> FormatIds { get; }

    /// <summary>
    /// Return any domain-rule violations found in <paramref name="model"/>.
    /// Each diagnostic's <see cref="ClipParseDiagnostic.Location"/> must use
    /// the same XPath-style grammar <see cref="Parsing.XmlRoundTripDiff"/>
    /// emits (<c>/Root/Child[n]/@Attr</c>) — the Problems panel resolves it
    /// back to XML via <see cref="Parsing.ClipParseLocationResolver"/>, which
    /// only understands that grammar.
    /// </summary>
    IReadOnlyList<ClipParseDiagnostic> Validate(ClipModel model);
}
