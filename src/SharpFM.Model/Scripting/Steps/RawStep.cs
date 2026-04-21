using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Forward-compatibility fallback for step elements whose <c>name</c>
/// attribute isn't in the catalog — e.g. a future FileMaker release
/// introduces a step we don't yet know about. Wraps a cloned source
/// <see cref="XElement"/> and round-trips it verbatim.
///
/// <para>
/// All known catalog steps are backed by typed POCOs. A <see cref="RawStep"/>
/// in the output therefore signals either an unknown step name or a
/// malformed element that slipped past the factory. Editable display
/// text is not supported because there's no shape contract to parse
/// against — the instance is sealed.
/// </para>
/// </summary>
public sealed class RawStep : ScriptStep
{
    private readonly XElement _element;

    /// <summary>
    /// The verbatim XML element for this step. Exposed as
    /// <c>internal</c> so callers inside the model assembly (e.g.
    /// <see cref="FmScript"/>'s apply operations) can construct mutated
    /// replacements, while external consumers remain insulated from XML.
    /// </summary>
    internal XElement Element => _element;

    public RawStep(XElement element, StepDefinition? definition)
        : base(definition, IsEnabled(element))
    {
        _element = new XElement(element);
    }

    public override XElement ToXml() => new XElement(_element);

    /// <summary>
    /// RawSteps are sealed in the display editor — there's no typed
    /// shape contract to parse edits against. They remain fully lossless
    /// at the XML level; they just can't be edited as display text.
    /// </summary>
    public override bool IsFullyEditable => false;

    public override string ToDisplayLine()
    {
        if (Definition == null)
        {
            // Non-catalog step: show whatever name the source XML carried,
            // or its preserved RawText body if that's all we have.
            var rawText = _element.Element("RawText")?.Value;
            if (!string.IsNullOrEmpty(rawText)) return rawText;
            return _element.Attribute("name")?.Value ?? "Unknown";
        }

        return CatalogDisplayRenderer.Render(_element, Definition);
    }

    public override List<ScriptDiagnostic> Validate(int lineIndex)
    {
        if (Definition == null)
        {
            var name = _element.Attribute("name")?.Value ?? "Unknown";
            return new List<ScriptDiagnostic>
            {
                new(lineIndex, 0, name.Length,
                    $"Unknown script step: '{name}'",
                    DiagnosticSeverity.Error)
            };
        }

        return CatalogValidator.Validate(_element, Definition, lineIndex);
    }

    private static bool IsEnabled(XElement element) =>
        element.Attribute("enable")?.Value != "False";
}
