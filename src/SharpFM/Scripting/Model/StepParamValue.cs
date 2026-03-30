using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace SharpFM.Scripting.Model;

public class StepParamValue
{
    public StepParam Definition { get; }
    public string? Value { get; set; }

    public StepParamValue(StepParam definition, string? value = null)
    {
        Definition = definition;
        Value = value;
    }

    public static StepParamValue FromXml(XElement stepEl, StepParam paramDef)
    {
        var element = FindParamElement(stepEl, paramDef);
        if (element == null)
            return new StepParamValue(paramDef);

        var value = paramDef.Type switch
        {
            "calculation" or "calc" => element.Value is { Length: > 0 } v ? v : null,
            "namedCalc" => element.Value is { Length: > 0 } v ? v : null,
            "text" => element.Value,
            "boolean" or "flagBoolean" or "flagElement" => ExtractBoolean(element, paramDef),
            "enum" => ExtractEnum(element, paramDef),
            "field" or "fieldOrVariable" => ExtractField(element),
            "script" or "layout" or "layoutRef"
                or "tableOccurrence" or "tableRef" or "tableReference" => ExtractNamedRef(element),
            _ => element.Value is { Length: > 0 } v ? v : null
        };

        return new StepParamValue(paramDef, value);
    }

    public XElement? ToXml()
    {
        return Definition.Type switch
        {
            "calculation" or "calc" => BuildCalculationXml(),
            "namedCalc" => BuildNamedCalcXml(),
            "text" => Value != null
                ? new XElement(Definition.XmlElement, XmlHelpers.XmlEscape(Value))
                : null,
            "boolean" or "flagBoolean" or "flagElement" => BuildBooleanXml(),
            "enum" => BuildEnumXml(),
            "field" or "fieldOrVariable" => BuildFieldXml(),
            "script" or "layout" or "layoutRef"
                or "tableOccurrence" or "tableRef" or "tableReference" => BuildNamedRefXml(),
            _ => null
        };
    }

    public string? ToDisplayString()
    {
        if (Value == null) return null;

        var label = Definition.HrLabel
            ?? (Definition.Type == "namedCalc" && Definition.WrapperElement != null
                ? Definition.WrapperElement : null);

        return label != null ? $"{label}: {Value}" : Value;
    }

    public List<ScriptDiagnostic> Validate(int line)
    {
        var diagnostics = new List<ScriptDiagnostic>();
        if (Value == null) return diagnostics;

        // Only validate enum/boolean params that have an explicit label.
        // Unlabeled params may have been positionally matched to a calculation value,
        // and we don't want to flag "$x > 0" as invalid for a boolean Restore param.
        var label = Definition.HrLabel
            ?? (Definition.Type == "namedCalc" && Definition.WrapperElement != null
                ? Definition.WrapperElement : null);

        var validValues = ScriptValidator.GetValidValues(Definition);
        if (validValues.Count > 0 && label != null
            && !validValues.Contains(Value, StringComparer.OrdinalIgnoreCase))
        {
            diagnostics.Add(new ScriptDiagnostic(
                line, 0, 0,
                $"Invalid value '{Value}' for {label}. Expected: {string.Join(", ", validValues)}",
                DiagnosticSeverity.Warning));
        }

        if (Definition.Type is "field" or "fieldOrVariable" && !string.IsNullOrEmpty(Value))
        {
            if (!Value.Contains("::") && !Value.StartsWith("$"))
            {
                diagnostics.Add(new ScriptDiagnostic(
                    line, 0, 0,
                    $"Expected field reference (Table::Field) or variable ($var), got '{Value}'",
                    DiagnosticSeverity.Warning));
            }
        }

        return diagnostics;
    }

    // --- Extraction helpers ---

    private static XElement? FindParamElement(XElement step, StepParam param)
    {
        if (param.ParentElement != null)
            return step.Element(param.ParentElement)?.Element(param.XmlElement);
        if (param.WrapperElement != null)
            return step.Element(param.WrapperElement)?.Element(param.XmlElement);
        return step.Element(param.XmlElement);
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
        var table = element.Attribute("table")?.Value;
        var name = element.Attribute("name")?.Value;
        if (!string.IsNullOrEmpty(table) && !string.IsNullOrEmpty(name))
            return $"{table}::{name}";
        var text = element.Value;
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static string? ExtractNamedRef(XElement element)
    {
        var name = element.Attribute("name")?.Value;
        return string.IsNullOrEmpty(name) ? null : $"\"{name}\"";
    }

    // --- Building helpers ---

    private XElement BuildCalculationXml()
    {
        return XElement.Parse($"<{Definition.XmlElement}><![CDATA[{Value ?? ""}]]></{Definition.XmlElement}>");
    }

    private XElement BuildNamedCalcXml()
    {
        var wrapper = Definition.WrapperElement ?? Definition.XmlElement;
        return XElement.Parse($"<{wrapper}><Calculation><![CDATA[{Value ?? ""}]]></Calculation></{wrapper}>");
    }

    private XElement? BuildBooleanXml()
    {
        var attr = Definition.XmlAttr ?? "state";
        string xmlValue;

        if (Value == null)
        {
            xmlValue = Definition.DefaultValue ?? "False";
        }
        else if (Definition.HrEnumValues != null)
        {
            xmlValue = Definition.HrEnumValues
                .FirstOrDefault(kv => kv.Value != null && kv.Value.Equals(Value, StringComparison.OrdinalIgnoreCase)).Key
                ?? Definition.DefaultValue ?? "False";
        }
        else if (Definition.InvertedHr)
        {
            xmlValue = Value.Equals("On", StringComparison.OrdinalIgnoreCase) ? "False" : "True";
        }
        else
        {
            xmlValue = Value.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                       Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? "True" : "False";
        }

        return new XElement(Definition.XmlElement, new XAttribute(attr, xmlValue));
    }

    private XElement? BuildEnumXml()
    {
        if (Value == null && Definition.DefaultValue == null) return null;
        var attr = Definition.XmlAttr ?? "value";
        var xmlValue = Value ?? Definition.DefaultValue ?? "";

        if (Definition.HrEnumValues != null)
        {
            var reverse = Definition.HrEnumValues
                .FirstOrDefault(kv => kv.Value != null && kv.Value.Equals(xmlValue, StringComparison.OrdinalIgnoreCase)).Key;
            if (reverse != null) xmlValue = reverse;
        }

        return new XElement(Definition.XmlElement, new XAttribute(attr, xmlValue));
    }

    private XElement BuildFieldXml()
    {
        if (Value == null)
            return XElement.Parse($"<{Definition.XmlElement} table=\"\" id=\"0\" name=\"\"/>");

        if (Value.Contains("::"))
        {
            var parts = Value.Split("::", 2);
            return new XElement(Definition.XmlElement,
                new XAttribute("table", parts[0]),
                new XAttribute("id", "0"),
                new XAttribute("name", parts[1]));
        }

        // Variable reference
        return new XElement(Definition.XmlElement, Value);
    }

    private XElement BuildNamedRefXml()
    {
        var name = Value != null ? XmlHelpers.Unquote(Value) : "";
        return new XElement(Definition.XmlElement,
            new XAttribute("id", "0"),
            new XAttribute("name", name));
    }
}
