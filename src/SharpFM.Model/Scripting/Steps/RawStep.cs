using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Transitional catch-all step type that wraps a cloned source
/// <see cref="XElement"/> for any script step that does not yet have a
/// typed POCO. Serialization is verbatim (the element round-trips byte
/// for byte, modulo whitespace), and display / validation delegate to
/// the stateless catalog-driven helpers.
///
/// <para>
/// <see cref="RawStep"/> is the only class in the domain that holds an
/// <see cref="XElement"/>. Every typed step (<c>GoToLayoutStep</c> and
/// siblings added in later phases) owns typed fields and never reaches
/// for raw XML. As more steps migrate to typed POCOs, <see cref="RawStep"/>
/// carries fewer and fewer catalog entries, but it remains useful as the
/// lossless fallback for unknown and long-tail steps.
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
