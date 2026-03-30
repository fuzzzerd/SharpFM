using System;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.Renderers;

public class PerformScriptRenderer : IMultiStepRenderer
{
    public string[] StepNames => ["Perform Script"];

    public string ToHr(XElement step, StepDefinition definition)
    {
        var scriptEl = step.Element("Script");
        var scriptName = scriptEl?.Attribute("name")?.Value;
        var param = step.Element("Calculation")?.Value;

        var parts = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(scriptName))
            parts.Add($"\"{scriptName}\"");
        if (!string.IsNullOrEmpty(param))
            parts.Add($"Parameter: {param}");

        if (parts.Count == 0)
            return "Perform Script";

        return $"Perform Script [ {string.Join(" ; ", parts)} ]";
    }

    public string ToXml(ParsedLine line, StepDefinition definition)
    {
        var enable = line.Disabled ? "False" : "True";
        string scriptName = "";
        string param = "";

        foreach (var p in line.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Parameter:", StringComparison.OrdinalIgnoreCase))
                param = trimmed.Substring(10).TrimStart();
            else
                scriptName = GenericStepRenderer.Unquote(trimmed);
        }

        return $"<Step enable=\"{enable}\" id=\"1\" name=\"Perform Script\">"
            + $"<Calculation><![CDATA[{param}]]></Calculation>"
            + $"<Script id=\"0\" name=\"{GenericStepRenderer.XmlEscape(scriptName)}\"/>"
            + "</Step>";
    }
}
