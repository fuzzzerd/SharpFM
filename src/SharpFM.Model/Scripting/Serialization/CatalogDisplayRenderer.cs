using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Stateless catalog-driven display text renderer for script steps that
/// don't yet have a typed POCO. Takes a step's source <see cref="XElement"/>
/// together with its <see cref="StepDefinition"/> and produces the
/// canonical display line (e.g. <c>Set Error Capture [ On ]</c>).
///
/// <para>
/// This replaces the old generic path that routed through
/// <c>StepParamValue.ToDisplayString</c>. The behavior is intentionally
/// identical so that unmigrated steps render the same before and after
/// the domain model refactor.
/// </para>
///
/// <para>
/// The comment step is handled as a special case: <c># (comment)</c>
/// renders as <c># {text}</c> without brackets, matching the old
/// behavior on <c>ScriptStep.ToDisplayLine</c>.
/// </para>
/// </summary>
internal static class CatalogDisplayRenderer
{
    public static string Render(XElement stepEl, StepDefinition def)
    {
        // Comments use a special display shape: "# text" without brackets.
        if (def.Name == "# (comment)")
        {
            var text = stepEl.Element("Text")?.Value ?? "";
            return $"# {text}";
        }

        var parts = def.Params
            .Select(p => RenderParam(stepEl, p))
            .Where(s => s != null)
            .ToList();

        if (parts.Count == 0)
            return def.Name;

        return $"{def.Name} [ {string.Join(" ; ", parts)} ]";
    }

    private static string? RenderParam(XElement stepEl, StepParam paramDef)
    {
        var value = CatalogParamExtractor.Extract(stepEl, paramDef);
        if (value == null) return null;

        if (paramDef.Type == "complex")
            return FormatComplexForDisplay(value, paramDef);

        var label = paramDef.HrLabel
            ?? (paramDef.Type == "namedCalc" && paramDef.WrapperElement != null
                ? paramDef.WrapperElement : null);

        return label != null ? $"{label}: {value}" : value;
    }

    /// <summary>
    /// Render a complex XML param as a human-readable summary. Extracts
    /// calculation values, field references, and element names from the
    /// preserved inner XML structure.
    /// </summary>
    private static string FormatComplexForDisplay(string innerXml, StepParam paramDef)
    {
        var label = paramDef.HrLabel ?? paramDef.WrapperElement ?? paramDef.XmlElement;
        try
        {
            var wrapper = XElement.Parse($"<root>{innerXml}</root>");
            var parts = new List<string>();

            foreach (var child in wrapper.Elements())
            {
                var calc = child.Descendants("Calculation").FirstOrDefault()?.Value;
                var name = child.Attribute("name")?.Value;
                var fieldName = child.Descendants("Field").FirstOrDefault()?.Attribute("name")?.Value;
                var fieldTable = child.Descendants("Field").FirstOrDefault()?.Attribute("table")?.Value;

                if (fieldTable is not null && fieldName is not null)
                    parts.Add($"{fieldTable}::{fieldName}");
                else if (calc is not null)
                    parts.Add(calc);
                else if (name is not null)
                    parts.Add(name);
                else
                    parts.Add(child.Name.LocalName);
            }

            return parts.Count > 0
                ? $"{label}: {string.Join(", ", parts)}"
                : $"{label}: (empty)";
        }
        catch
        {
            return $"{label}: {innerXml}";
        }
    }
}
