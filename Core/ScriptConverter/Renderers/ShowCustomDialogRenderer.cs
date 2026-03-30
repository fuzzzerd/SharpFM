using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.Renderers;

public class ShowCustomDialogRenderer : IMultiStepRenderer
{
    public string[] StepNames => ["Show Custom Dialog"];

    public string ToHr(XElement step, StepDefinition definition)
    {
        var title = step.Element("Title")?.Element("Calculation")?.Value;
        var message = step.Element("Message")?.Element("Calculation")?.Value;

        var buttons = step.Element("Buttons")?.Elements("Button")
            .Select(b => b.Element("Calculation")?.Value)
            .Where(b => !string.IsNullOrEmpty(b))
            .ToList() ?? new List<string?>();

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(title))
            parts.Add($"Title: {title}");
        if (!string.IsNullOrEmpty(message))
            parts.Add($"Message: {message}");
        if (buttons.Count > 0)
            parts.Add($"Buttons: {string.Join(", ", buttons)}");

        if (parts.Count == 0)
            return "Show Custom Dialog";

        return $"Show Custom Dialog [ {string.Join(" ; ", parts)} ]";
    }

    public string ToXml(ParsedLine line, StepDefinition definition)
    {
        var enable = line.Disabled ? "False" : "True";
        string title = "";
        string message = "";
        var buttons = new List<string>();

        foreach (var p in line.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Title:", System.StringComparison.OrdinalIgnoreCase))
                title = trimmed.Substring(6).TrimStart();
            else if (trimmed.StartsWith("Message:", System.StringComparison.OrdinalIgnoreCase))
                message = trimmed.Substring(8).TrimStart();
            else if (trimmed.StartsWith("Buttons:", System.StringComparison.OrdinalIgnoreCase))
            {
                var buttonList = trimmed.Substring(8).TrimStart();
                buttons.AddRange(buttonList.Split(',').Select(b => b.Trim()));
            }
        }

        var xml = $"<Step enable=\"{enable}\" id=\"87\" name=\"Show Custom Dialog\">"
            + $"<Title><Calculation><![CDATA[{title}]]></Calculation></Title>"
            + $"<Message><Calculation><![CDATA[{message}]]></Calculation></Message>";

        if (buttons.Count > 0)
        {
            xml += "<Buttons>";
            foreach (var btn in buttons)
                xml += $"<Button CommitState=\"True\"><Calculation><![CDATA[{btn}]]></Calculation></Button>";
            xml += "</Buttons>";
        }

        xml += "</Step>";
        return xml;
    }
}
