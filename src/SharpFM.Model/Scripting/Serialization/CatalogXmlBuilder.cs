using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Stateless catalog-driven XML construction for script steps that don't
/// yet have a typed POCO. Takes either a parameter value map (keyed by
/// catalog param name) or an array of display-text tokens and produces
/// the corresponding <c>&lt;Step&gt;</c> element.
///
/// <para>
/// Ports the retired <c>StepParamValue.ToXml</c> dispatch and the
/// <c>ScriptTextParser.MatchDisplayParams</c> label/positional matching
/// into pure functions, eliminating the need for a per-param wrapper
/// object on the build path.
/// </para>
///
/// <para>
/// Known limitation: display-text-originated named-ref params (layout,
/// script, tableOccurrence) have no id available at the display level, so
/// those elements are emitted with only a name attribute. Unmigrated
/// steps that round-trip entirely through the display-text editor lose
/// their ref ids. Steps that need lossless id preservation must migrate
/// to a typed POCO so their <c>FromDisplayParams</c> can parse a
/// <c>(#id)</c> suffix. This limitation is documented and accepted until
/// Phase 3 reaches each affected step.
/// </para>
/// </summary>
public static class CatalogXmlBuilder
{
    /// <summary>
    /// Build a complete <c>&lt;Step&gt;</c> element from a display-line
    /// token array, matching each catalog param to a token either by
    /// label or positionally.
    /// </summary>
    public static XElement BuildStep(StepDefinition def, bool enabled, string[] hrParams)
    {
        var values = MatchDisplayParams(hrParams, def);
        return BuildStepFromValues(def, enabled, values);
    }

    /// <summary>
    /// Build a complete <c>&lt;Step&gt;</c> element from a param-name ->
    /// value dict. Used by <c>FmScript.ApplyAdd</c> (MCP operations) where
    /// the caller supplies params by catalog name rather than display text.
    /// </summary>
    public static XElement BuildStepFromMap(
        StepDefinition def, bool enabled, IReadOnlyDictionary<string, string?>? paramMap)
    {
        // Positional list — not a dict — because distinct catalog params
        // can share an XmlElement (e.g. Show Custom Dialog's Title and
        // Message both use XmlElement="Calculation" and differ only in
        // WrapperElement). Keying by XmlElement would collide.
        var values = new List<(StepParam Param, string? Value)>(def.Params.Length);
        foreach (var paramDef in def.Params)
        {
            var key = paramDef.HrLabel ?? paramDef.WrapperElement ?? paramDef.XmlElement;
            string? value = null;
            paramMap?.TryGetValue(key, out value);
            values.Add((paramDef, value));
        }
        return BuildStepFromValues(def, enabled, values);
    }

    /// <summary>
    /// Return a new step element with the named param replaced by a new
    /// value. Used by <c>FmScript.ApplyUpdate</c> (MCP operations) to
    /// update a single param without reconstructing the whole step.
    /// Param name may be either the HrLabel, WrapperElement, or XmlElement.
    /// </summary>
    public static XElement? UpdateParam(XElement stepEl, StepDefinition def, string paramName, string? newValue)
    {
        var paramDef = def.Params.FirstOrDefault(p =>
        {
            var name = p.HrLabel ?? p.WrapperElement ?? p.XmlElement;
            return string.Equals(name, paramName, StringComparison.OrdinalIgnoreCase);
        });
        if (paramDef == null) return null;

        var updated = new XElement(stepEl);

        // Remove the existing element for this param (if any) and re-emit
        // with the new value. This keeps the build logic in one place and
        // avoids subtle mutation of inner structure.
        var existing = CatalogParamExtractor.FindParamElement(updated, paramDef);
        existing?.Remove();

        var newElement = BuildParamElement(paramDef, newValue);
        if (newElement != null)
        {
            if (paramDef.ParentElement != null || paramDef.WrapperElement != null)
            {
                var wrapperName = paramDef.ParentElement ?? paramDef.WrapperElement!;
                var wrapper = updated.Element(wrapperName);
                if (wrapper == null)
                {
                    wrapper = new XElement(wrapperName);
                    updated.Add(wrapper);
                }
                wrapper.Add(newElement);
            }
            else
            {
                updated.Add(newElement);
            }
        }

        return updated;
    }

    // --- Display-param matching (ported from ScriptTextParser.MatchDisplayParams) ---

    /// <summary>
    /// Match display-text tokens to catalog params, label-first then
    /// positionally. Returns a positional list so distinct params
    /// sharing an XmlElement (e.g. Show Custom Dialog Title / Message)
    /// are preserved as independent slots.
    /// </summary>
    private static List<(StepParam Param, string? Value)> MatchDisplayParams(
        string[] hrParams, StepDefinition definition)
    {
        var result = new List<(StepParam, string?)>(definition.Params.Length);
        var used = new bool[hrParams.Length];

        foreach (var paramDef in definition.Params)
        {
            var label = paramDef.HrLabel
                ?? (paramDef.Type == "namedCalc" && paramDef.WrapperElement != null
                    ? paramDef.WrapperElement : null);

            string? value = null;

            if (label != null)
            {
                for (int i = 0; i < hrParams.Length; i++)
                {
                    if (used[i]) continue;
                    var trimmed = hrParams[i].TrimStart();
                    if (trimmed.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
                    {
                        value = trimmed.Substring(label.Length + 1).TrimStart();
                        used[i] = true;
                        break;
                    }
                }
            }

            if (value == null)
            {
                for (int i = 0; i < hrParams.Length; i++)
                {
                    if (used[i]) continue;
                    value = hrParams[i].Trim();
                    used[i] = true;
                    break;
                }
            }

            result.Add((paramDef, value));
        }

        return result;
    }

    // --- Step assembly ---

    private static XElement BuildStepFromValues(
        StepDefinition def, bool enabled, IReadOnlyList<(StepParam Param, string? Value)> values)
    {
        var id = def.Id ?? 89;
        var step = new XElement("Step",
            new XAttribute("enable", enabled ? "True" : "False"),
            new XAttribute("id", id),
            new XAttribute("name", def.Name));

        if (def.SelfClosing && values.All(v => v.Value == null))
            return step;

        foreach (var (paramDef, value) in values)
        {
            var element = BuildParamElement(paramDef, value);
            if (element == null) continue;

            if (paramDef.ParentElement != null || paramDef.WrapperElement != null)
            {
                var wrapperName = paramDef.ParentElement ?? paramDef.WrapperElement!;
                // Per-slot wrapper: Title and Message are different wrapper
                // elements but share XmlElement="Calculation", so we add
                // each to its own wrapper. If a step really wants multiple
                // children under the same wrapper, reuse it.
                var wrapper = step.Element(wrapperName);
                if (wrapper == null)
                {
                    wrapper = new XElement(wrapperName);
                    step.Add(wrapper);
                }
                wrapper.Add(element);
            }
            else
            {
                step.Add(element);
            }
        }

        return step;
    }

    // --- Per-param element construction (ported from StepParamValue builders) ---

    private static XElement? BuildParamElement(StepParam paramDef, string? value)
    {
        return paramDef.Type switch
        {
            "calculation" or "calc" => BuildCalculationXml(paramDef, value),
            "namedCalc" => BuildNamedCalcXml(paramDef, value),
            "text" => value != null
                ? new XElement(paramDef.XmlElement, XmlHelpers.XmlEscape(value))
                : null,
            "boolean" or "flagBoolean" or "flagElement" => BuildBooleanXml(paramDef, value),
            "enum" => BuildEnumXml(paramDef, value),
            "field" or "fieldOrVariable" => BuildFieldXml(paramDef, value),
            "script" or "layout" or "layoutRef"
                or "tableOccurrence" or "tableRef" or "tableReference" => BuildNamedRefXml(paramDef, value),
            "complex" => BuildComplexXml(paramDef, value),
            _ => null
        };
    }

    private static XElement BuildCalculationXml(StepParam paramDef, string? value) =>
        XElement.Parse($"<{paramDef.XmlElement}><![CDATA[{value ?? ""}]]></{paramDef.XmlElement}>");

    private static XElement BuildNamedCalcXml(StepParam paramDef, string? value)
    {
        // namedCalc wraps the Calculation inside a named wrapper element
        // (e.g., <Value><Calculation>...</Calculation></Value>). The wrapper
        // is emitted separately by BuildStepFromValues via ParentElement,
        // so here we just emit the inner Calculation child keyed by
        // paramDef.XmlElement — which for namedCalc is "Calculation".
        return XElement.Parse($"<{paramDef.XmlElement}><![CDATA[{value ?? ""}]]></{paramDef.XmlElement}>");
    }

    private static XElement? BuildBooleanXml(StepParam paramDef, string? value)
    {
        var attr = paramDef.XmlAttr ?? "state";
        string xmlValue;

        if (value == null)
        {
            xmlValue = paramDef.DefaultValue ?? "False";
        }
        else if (paramDef.HrEnumValues != null)
        {
            xmlValue = paramDef.HrEnumValues
                .FirstOrDefault(kv => kv.Value != null && kv.Value.Equals(value, StringComparison.OrdinalIgnoreCase)).Key
                ?? paramDef.DefaultValue ?? "False";
        }
        else if (paramDef.InvertedHr)
        {
            xmlValue = value.Equals("On", StringComparison.OrdinalIgnoreCase) ? "False" : "True";
        }
        else
        {
            xmlValue = value.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                       value.Equals("True", StringComparison.OrdinalIgnoreCase) ? "True" : "False";
        }

        return new XElement(paramDef.XmlElement, new XAttribute(attr, xmlValue));
    }

    private static XElement? BuildEnumXml(StepParam paramDef, string? value)
    {
        if (value == null && paramDef.DefaultValue == null) return null;
        var attr = paramDef.XmlAttr ?? "value";
        var xmlValue = value ?? paramDef.DefaultValue ?? "";

        if (paramDef.HrEnumValues != null)
        {
            var reverse = paramDef.HrEnumValues
                .FirstOrDefault(kv => kv.Value != null && kv.Value.Equals(xmlValue, StringComparison.OrdinalIgnoreCase)).Key;
            if (reverse != null) xmlValue = reverse;
        }

        return new XElement(paramDef.XmlElement, new XAttribute(attr, xmlValue));
    }

    private static XElement BuildFieldXml(StepParam paramDef, string? value)
    {
        // Empty / null → degenerate empty field slot.
        if (string.IsNullOrEmpty(value))
            return XElement.Parse($"<{paramDef.XmlElement} table=\"\" id=\"0\" name=\"\"/>");

        // Delegate to FieldRef so the display-text (#id) suffix — if the
        // user or source XML provided one — round-trips through the generic
        // catalog path. Unresolved refs get id=0 (same sentinel as before);
        // future DDL-dictionary integration can resolve those downstream.
        return Values.FieldRef.FromDisplayToken(value).ToXml(paramDef.XmlElement);
    }

    private static XElement BuildNamedRefXml(StepParam paramDef, string? value)
    {
        // Display-text-originated named refs have no id at this layer.
        // Emit only the name attribute — the id=0 hardcode is gone.
        // Typed POCOs for named-ref-bearing steps (GoToLayoutStep, etc.)
        // construct these elements directly with the correct id.
        var name = value != null ? XmlHelpers.Unquote(value) : "";
        return new XElement(paramDef.XmlElement, new XAttribute("name", name));
    }

    private static XElement? BuildComplexXml(StepParam paramDef, string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        try
        {
            var xmlElement = paramDef.WrapperElement ?? paramDef.XmlElement;
            return XElement.Parse($"<{xmlElement}>{value}</{xmlElement}>");
        }
        catch
        {
            return null;
        }
    }
}
