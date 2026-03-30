using System;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.Renderers;

public class SetVariableRenderer : IMultiStepRenderer
{
    public string[] StepNames => ["Set Variable"];

    public string ToHr(XElement step, StepDefinition definition)
    {
        var name = step.Element("Name")?.Value ?? "";
        var value = step.Element("Value")?.Element("Calculation")?.Value;
        var repetition = step.Element("Repetition")?.Element("Calculation")?.Value;

        // Handle array syntax: $var[n]
        var displayName = name;
        if (!string.IsNullOrEmpty(repetition) && repetition != "1")
            displayName = $"{name}[{repetition}]";

        if (string.IsNullOrEmpty(value))
            return $"Set Variable [ {displayName} ]";

        return $"Set Variable [ {displayName} ; Value: {value} ]";
    }

    public string ToXml(ParsedLine line, StepDefinition definition)
    {
        var enable = line.Disabled ? "False" : "True";
        string varName = "";
        string calcValue = "";
        string repetition = "1";

        foreach (var p in line.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Value:", StringComparison.OrdinalIgnoreCase))
            {
                calcValue = trimmed.Substring(6).TrimStart();
            }
            else if (trimmed.StartsWith("$") || trimmed.StartsWith("$$"))
            {
                var parsed = ParseVarRepetition(trimmed);
                varName = parsed.Name;
                repetition = parsed.Repetition;
            }
        }

        return $"<Step enable=\"{enable}\" id=\"141\" name=\"Set Variable\">"
            + $"<Value><Calculation><![CDATA[{calcValue}]]></Calculation></Value>"
            + $"<Repetition><Calculation><![CDATA[{repetition}]]></Calculation></Repetition>"
            + $"<Name>{GenericStepRenderer.XmlEscape(varName)}</Name>"
            + "</Step>";
    }

    internal static (string Name, string Repetition) ParseVarRepetition(string text)
    {
        var bracketStart = text.IndexOf('[');
        if (bracketStart > 0 && text.EndsWith(']'))
        {
            var name = text.Substring(0, bracketStart);
            var rep = text.Substring(bracketStart + 1, text.Length - bracketStart - 2);
            return (name, rep);
        }
        return (text, "1");
    }
}
