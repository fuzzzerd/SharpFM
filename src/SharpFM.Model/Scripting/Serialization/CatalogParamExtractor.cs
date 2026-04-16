using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Stateless extraction of per-param string values from a step's XML
/// element, driven by catalog metadata. These helpers replace the old
/// <c>StepParamValue.FromXml</c> behavior as pure functions so callers
/// (display rendering, validation, XML build) can work directly against
/// <see cref="XElement"/> without materializing an intermediate
/// string-valued param object.
///
/// <para>
/// The dispatch on <see cref="StepParam.Type"/> and the individual
/// <c>Extract*</c> helpers are ported verbatim from the retired
/// <c>StepParamValue</c> so that unmigrated catalog steps render and
/// validate identically through the new pipeline.
/// </para>
/// </summary>
internal static class CatalogParamExtractor
{
    /// <summary>
    /// Locate the XML element corresponding to a catalog param, honoring
    /// both the <see cref="StepParam.ParentElement"/> and
    /// <see cref="StepParam.WrapperElement"/> conventions used throughout
    /// the FileMaker step catalog.
    /// </summary>
    public static XElement? FindParamElement(XElement stepEl, StepParam paramDef)
    {
        if (paramDef.ParentElement != null)
            return stepEl.Element(paramDef.ParentElement)?.Element(paramDef.XmlElement);
        if (paramDef.WrapperElement != null)
            return stepEl.Element(paramDef.WrapperElement)?.Element(paramDef.XmlElement);
        return stepEl.Element(paramDef.XmlElement);
    }

    /// <summary>
    /// Extract the display-string value for a param from its XML element,
    /// dispatching on the catalog-declared type. Returns null when the
    /// param element is absent or the type produces no meaningful value.
    /// </summary>
    public static string? Extract(XElement stepEl, StepParam paramDef)
    {
        var element = FindParamElement(stepEl, paramDef);
        if (element == null) return null;

        return paramDef.Type switch
        {
            "calculation" or "calc" => element.Value is { Length: > 0 } v ? v : null,
            "namedCalc" => element.Value is { Length: > 0 } v ? v : null,
            "text" => element.Value,
            "boolean" or "flagBoolean" or "flagElement" => ExtractBoolean(element, paramDef),
            "enum" => ExtractEnum(element, paramDef),
            "field" or "fieldOrVariable" => ExtractField(element),
            "script" or "layout" or "layoutRef"
                or "tableOccurrence" or "tableRef" or "tableReference" => ExtractNamedRef(element),
            "complex" => ExtractComplex(element),
            _ => element.Value is { Length: > 0 } v ? v : null
        };
    }

    private static string? ExtractBoolean(XElement element, StepParam param)
    {
        var attr = param.XmlAttr ?? "state";
        var raw = element.Attribute(attr)?.Value;
        if (raw == null) return null;

        if (param.HrEnumValues != null && param.HrEnumValues.TryGetValue(raw, out var hrVal))
            return hrVal;
        if (param.InvertedHr)
            return raw == "True" ? "Off" : "On";
        if (param.HrValues is { Length: >= 2 })
            return raw == "True" ? param.HrValues[0] : param.HrValues[1];
        return raw == "True" ? "On" : "Off";
    }

    private static string? ExtractEnum(XElement element, StepParam param)
    {
        var attr = param.XmlAttr ?? "value";
        var raw = element.Attribute(attr)?.Value ?? element.Value;
        if (string.IsNullOrEmpty(raw)) return null;
        if (param.HrEnumValues != null && param.HrEnumValues.TryGetValue(raw, out var hrVal))
            return hrVal;
        return raw;
    }

    private static string? ExtractField(XElement element)
    {
        // Delegate to FieldRef so every field-param render uses the same
        // lossless display form (Table::Name (#id)) as typed POCOs like
        // SetFieldStep and ShowCustomDialogStep.
        var fr = Values.FieldRef.FromXml(element);
        if (fr.IsVariable)
            return string.IsNullOrEmpty(fr.VariableName) ? null : fr.VariableName;
        if (string.IsNullOrEmpty(fr.Name))
            return null;
        return fr.ToDisplayString();
    }

    /// <summary>
    /// Capture the inner XML of a complex param as a string. Complex
    /// structures (Buttons, InputFields, SortList, etc.) are preserved
    /// verbatim so they can be reconstructed without loss during ToXml.
    /// </summary>
    private static string? ExtractComplex(XElement element)
    {
        if (!element.HasElements) return null;
        return string.Concat(element.Elements().Select(e => e.ToString()));
    }

    private static string? ExtractNamedRef(XElement element)
    {
        var name = element.Attribute("name")?.Value;
        return string.IsNullOrEmpty(name) ? null : $"\"{name}\"";
    }
}
