using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter;

public class GenericStepRenderer : IStepRenderer
{
    public string ToHr(XElement step, StepDefinition definition)
    {
        var parts = new List<string>();

        foreach (var param in definition.Params)
        {
            var value = ExtractParamValue(step, param);
            if (value == null)
                continue;

            var label = param.HrLabel
                ?? (param.Type == "namedCalc" && param.WrapperElement != null ? param.WrapperElement : null);

            if (label != null)
                parts.Add($"{label}: {value}");
            else
                parts.Add(value);
        }

        if (parts.Count == 0)
            return definition.Name;

        return $"{definition.Name} [ {string.Join(" ; ", parts)} ]";
    }

    public string ToXml(ParsedLine line, StepDefinition definition)
    {
        var sb = new StringBuilder();
        var enable = line.Disabled ? "False" : "True";
        sb.Append($"<Step enable=\"{enable}\" id=\"{definition.Id ?? 0}\" name=\"{XmlEscape(definition.Name)}\"");

        if (definition.SelfClosing && line.Params.Length == 0)
        {
            sb.Append("/>");
            return sb.ToString();
        }

        sb.Append('>');

        var matchedParams = MatchParams(line.Params, definition.Params);
        for (int i = 0; i < definition.Params.Length; i++)
        {
            var param = definition.Params[i];
            var value = i < matchedParams.Length ? matchedParams[i] : null;
            var xml = BuildParamXml(param, value);
            if (xml != null)
                sb.Append(xml);
        }

        sb.Append("</Step>");
        return sb.ToString();
    }

    private static string? ExtractParamValue(XElement step, StepParam param)
    {
        var element = FindParamElement(step, param);
        if (element == null)
            return null;

        return param.Type switch
        {
            "calculation" or "calc" => ExtractCalculation(element),
            "namedCalc" => ExtractCalculation(element),
            "text" => element.Value,
            "boolean" => ExtractBoolean(element, param),
            "flagBoolean" or "flagElement" => ExtractBoolean(element, param),
            "enum" => ExtractEnum(element, param),
            "field" or "fieldOrVariable" => ExtractField(element),
            "script" => ExtractNamedRef(element),
            "layout" or "layoutRef" => ExtractNamedRef(element),
            "tableOccurrence" or "tableRef" or "tableReference" => ExtractNamedRef(element),
            _ => element.Value.Length > 0 ? element.Value : null
        };
    }

    private static XElement? FindParamElement(XElement step, StepParam param)
    {
        if (param.ParentElement != null)
        {
            var parent = step.Element(param.ParentElement);
            return parent?.Element(param.XmlElement);
        }
        // namedCalc params use wrapperElement as the container in XML
        if (param.WrapperElement != null)
        {
            var wrapper = step.Element(param.WrapperElement);
            return wrapper?.Element(param.XmlElement);
        }
        return step.Element(param.XmlElement);
    }

    private static string? ExtractCalculation(XElement? element)
    {
        if (element == null) return null;
        var value = element.Value;
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string? ExtractBoolean(XElement element, StepParam param)
    {
        var attr = param.XmlAttr ?? "state";
        var raw = element.Attribute(attr)?.Value;
        if (raw == null) return null;

        if (param.HrEnumValues != null && param.HrEnumValues.TryGetValue(raw, out var hrVal))
            return hrVal;

        if (param.InvertedHr)
            raw = raw == "True" ? "Off" : "On";
        else if (param.HrValues is { Length: >= 2 })
            raw = raw == "True" ? param.HrValues[0] : param.HrValues[1];
        else
            raw = raw == "True" ? "On" : "Off";

        return raw;
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

        // Variable reference stored as text content
        var text = element.Value;
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static string? ExtractNamedRef(XElement element)
    {
        var name = element.Attribute("name")?.Value;
        return string.IsNullOrEmpty(name) ? null : $"\"{name}\"";
    }

    internal static string?[] MatchParams(string[] hrParams, StepParam[] catalogParams)
    {
        var result = new string?[catalogParams.Length];
        var used = new bool[hrParams.Length];

        // First pass: match by label (including synthetic labels from wrapperElement)
        for (int ci = 0; ci < catalogParams.Length; ci++)
        {
            var label = catalogParams[ci].HrLabel
                ?? (catalogParams[ci].Type == "namedCalc" && catalogParams[ci].WrapperElement != null
                    ? catalogParams[ci].WrapperElement : null);
            if (label == null) continue;
            for (int hi = 0; hi < hrParams.Length; hi++)
            {
                if (used[hi]) continue;
                var stripped = StripLabel(hrParams[hi], label);
                if (stripped != null)
                {
                    result[ci] = stripped;
                    used[hi] = true;
                    break;
                }
            }
        }

        // Second pass: positional fill
        int nextHr = 0;
        for (int ci = 0; ci < catalogParams.Length; ci++)
        {
            if (result[ci] != null) continue;
            while (nextHr < hrParams.Length && used[nextHr]) nextHr++;
            if (nextHr < hrParams.Length)
            {
                result[ci] = hrParams[nextHr].Trim();
                used[nextHr] = true;
                nextHr++;
            }
        }

        return result;
    }

    private static string? StripLabel(string param, string label)
    {
        var trimmed = param.TrimStart();
        if (trimmed.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
            return trimmed.Substring(label.Length + 1).TrimStart();
        return null;
    }

    private static string? BuildParamXml(StepParam param, string? value)
    {
        return param.Type switch
        {
            "calculation" or "calc" => BuildCalculationXml(param, value),
            "namedCalc" => BuildNamedCalcXml(param, value),
            "text" => value != null ? $"<{param.XmlElement}>{XmlEscape(value)}</{param.XmlElement}>" : null,
            "boolean" or "flagBoolean" or "flagElement" => BuildBooleanXml(param, value),
            "enum" => BuildEnumXml(param, value),
            "field" or "fieldOrVariable" => BuildFieldXml(param, value),
            "script" => BuildNamedRefXml(param, value),
            "layout" or "layoutRef" => BuildNamedRefXml(param, value),
            "tableOccurrence" or "tableRef" or "tableReference" => BuildNamedRefXml(param, value),
            _ => null
        };
    }

    private static string BuildCalculationXml(StepParam param, string? value)
    {
        var calc = value ?? "";
        return $"<{param.XmlElement}><![CDATA[{calc}]]></{param.XmlElement}>";
    }

    private static string BuildNamedCalcXml(StepParam param, string? value)
    {
        var calc = value ?? "";
        var wrapper = param.WrapperElement ?? param.XmlElement;
        return $"<{wrapper}><Calculation><![CDATA[{calc}]]></Calculation></{wrapper}>";
    }

    private static string? BuildBooleanXml(StepParam param, string? value)
    {
        var attr = param.XmlAttr ?? "state";
        string xmlValue;

        if (value == null)
        {
            xmlValue = param.DefaultValue ?? "False";
        }
        else if (param.HrEnumValues != null)
        {
            // Reverse lookup: find xml value from HR value
            xmlValue = param.HrEnumValues
                .FirstOrDefault(kv => kv.Value != null && kv.Value.Equals(value, StringComparison.OrdinalIgnoreCase)).Key
                ?? param.DefaultValue ?? "False";
        }
        else if (param.InvertedHr)
        {
            xmlValue = value.Equals("On", StringComparison.OrdinalIgnoreCase) ? "False" : "True";
        }
        else
        {
            xmlValue = value.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                       value.Equals("True", StringComparison.OrdinalIgnoreCase) ? "True" : "False";
        }

        return $"<{param.XmlElement} {attr}=\"{xmlValue}\"/>";
    }

    private static string? BuildEnumXml(StepParam param, string? value)
    {
        if (value == null && param.DefaultValue == null) return null;
        var attr = param.XmlAttr ?? "value";
        var xmlValue = value ?? param.DefaultValue ?? "";

        // Reverse lookup for hrEnumValues
        if (param.HrEnumValues != null)
        {
            var reverse = param.HrEnumValues
                .FirstOrDefault(kv => kv.Value != null && kv.Value.Equals(xmlValue, StringComparison.OrdinalIgnoreCase)).Key;
            if (reverse != null)
                xmlValue = reverse;
        }

        return $"<{param.XmlElement} {attr}=\"{XmlEscape(xmlValue)}\"/>";
    }

    private static string? BuildFieldXml(StepParam param, string? value)
    {
        if (value == null) return $"<{param.XmlElement} table=\"\" id=\"0\" name=\"\"/>";

        if (value.Contains("::"))
        {
            var parts = value.Split("::", 2);
            return $"<{param.XmlElement} table=\"{XmlEscape(parts[0])}\" id=\"0\" name=\"{XmlEscape(parts[1])}\"/>";
        }

        // Variable reference
        return $"<{param.XmlElement}>{XmlEscape(value)}</{param.XmlElement}>";
    }

    private static string? BuildNamedRefXml(StepParam param, string? value)
    {
        var name = value != null ? Unquote(value) : "";
        return $"<{param.XmlElement} id=\"0\" name=\"{XmlEscape(name)}\"/>";
    }

    internal static string Unquote(string s)
    {
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            return s[1..^1];
        return s;
    }

    internal static string XmlEscape(string s)
    {
        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
    }
}
