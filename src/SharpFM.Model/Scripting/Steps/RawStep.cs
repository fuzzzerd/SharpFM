using System.Collections.Generic;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Forward-compatibility fallback for step elements whose <c>name</c>
/// attribute doesn't match any registered POCO — e.g. a future FileMaker
/// release introduces a step we don't yet know about. Wraps a cloned
/// source <see cref="XElement"/> and round-trips it verbatim.
///
/// <para>
/// A <see cref="RawStep"/> in the output signals an unknown step name.
/// Editable display text is not supported because there's no shape
/// contract to parse edits against — the instance is sealed; XML
/// round-trip remains lossless.
/// </para>
/// </summary>
public sealed class RawStep : ScriptStep
{
    private readonly XElement _element;

    /// <summary>
    /// The verbatim XML element for this step. Exposed as <c>internal</c>
    /// so model-assembly callers can introspect it; external consumers
    /// stay insulated from XML.
    /// </summary>
    internal XElement Element => _element;

    /// <summary>
    /// Step name as preserved from the source XML, e.g. <c>"Future Step"</c>.
    /// Returns <c>"Unknown"</c> if the source had no <c>name</c> attribute.
    /// </summary>
    public string Name => _element.Attribute("name")?.Value ?? "Unknown";

    public RawStep(XElement element)
        : base(IsEnabled(element))
    {
        _element = new XElement(element);
    }

    public override XElement ToXml() => new XElement(_element);

    public override bool IsFullyEditable => false;

    public override string ToDisplayLine()
    {
        var rawText = _element.Element("RawText")?.Value;
        if (!string.IsNullOrEmpty(rawText)) return rawText;
        return Name;
    }

    public override List<ScriptDiagnostic> Validate(int lineIndex) =>
        new()
        {
            new(lineIndex, 0, Name.Length,
                $"Unknown script step '{Name}' — preserved verbatim as a RawStep. "
                + "Edit the underlying XML via the XML editor; display-text edits here won't round-trip.",
                DiagnosticSeverity.Warning)
        };

    private static bool IsEnabled(XElement element) =>
        element.Attribute("enable")?.Value != "False";
}
