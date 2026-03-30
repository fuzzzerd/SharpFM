using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.Renderers;

public class GoToRecordRenderer : IMultiStepRenderer
{
    public string[] StepNames => ["Go to Record/Request/Page"];

    public string ToHr(XElement step, StepDefinition definition)
    {
        var location = step.Element("RowPageLocation")?.Attribute("value")?.Value;
        var exitAfterLast = step.Element("Exit")?.Attribute("state")?.Value;
        var calc = step.Element("Calculation")?.Value;

        var parts = new List<string>();

        if (location == "By Calculation" && !string.IsNullOrEmpty(calc))
            parts.Add($"By Calculation: {calc}");
        else if (!string.IsNullOrEmpty(location))
            parts.Add(location);

        if (exitAfterLast == "True")
            parts.Add("Exit after last: On");

        if (parts.Count == 0)
            return "Go to Record/Request/Page";

        return $"Go to Record/Request/Page [ {string.Join(" ; ", parts)} ]";
    }

    public string ToXml(ParsedLine line, StepDefinition definition)
    {
        var enable = line.Disabled ? "False" : "True";
        string location = "Next";
        string exitState = "False";
        string calc = "";

        foreach (var p in line.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Exit after last:", StringComparison.OrdinalIgnoreCase))
            {
                var val = trimmed.Substring(16).TrimStart();
                exitState = val.Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : "False";
            }
            else if (trimmed.StartsWith("By Calculation:", StringComparison.OrdinalIgnoreCase))
            {
                location = "By Calculation";
                calc = trimmed.Substring(15).TrimStart();
            }
            else if (trimmed is "First" or "Last" or "Previous" or "Next")
            {
                location = trimmed;
            }
        }

        var xml = $"<Step enable=\"{enable}\" id=\"16\" name=\"Go to Record/Request/Page\">"
            + $"<RowPageLocation value=\"{GenericStepRenderer.XmlEscape(location)}\"/>"
            + $"<Exit state=\"{exitState}\"/>";

        if (!string.IsNullOrEmpty(calc))
            xml += $"<Calculation><![CDATA[{calc}]]></Calculation>";

        xml += "</Step>";
        return xml;
    }
}
