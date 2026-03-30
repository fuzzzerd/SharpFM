using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter;

public static class XmlToHrConverter
{
    private const string Indent = "    ";

    private static readonly HashSet<string> IndentBefore = new(StringComparer.OrdinalIgnoreCase)
    {
        "End If", "End Loop", "Else", "Else If",
        "Commit Transaction", "Revert Transaction"
    };

    private static readonly HashSet<string> IndentAfter = new(StringComparer.OrdinalIgnoreCase)
    {
        "If", "Else If", "Else", "Loop",
        "Open Transaction", "Revert Transaction"
    };

    public static string Convert(string fmXml)
    {
        if (string.IsNullOrWhiteSpace(fmXml))
            return "";

        XDocument doc;
        try
        {
            doc = XDocument.Parse(fmXml);
        }
        catch
        {
            return "";
        }

        var steps = GetStepElements(doc);
        if (!steps.Any())
            return "";

        var sb = new StringBuilder();
        int indentLevel = 0;

        foreach (var step in steps)
        {
            var stepName = step.Attribute("name")?.Value ?? "";
            var enabled = step.Attribute("enable")?.Value != "False";

            // Decrease indent before close/middle blocks
            if (IndentBefore.Contains(stepName) && indentLevel > 0)
                indentLevel--;

            // Render the step
            var definition = LookupStep(step);
            var renderer = StepRendererRegistry.GetRenderer(stepName);
            var hrLine = renderer.ToHr(step, definition);

            // Apply disabled prefix
            if (!enabled)
                hrLine = $"// {hrLine}";

            // Apply indentation
            if (indentLevel > 0)
                hrLine = string.Concat(Enumerable.Repeat(Indent, indentLevel)) + hrLine;

            sb.AppendLine(hrLine);

            // Increase indent after open/middle blocks
            if (IndentAfter.Contains(stepName))
                indentLevel++;
        }

        // Remove trailing newline
        if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
            sb.Length--;
        if (sb.Length > 0 && sb[sb.Length - 1] == '\r')
            sb.Length--;

        return sb.ToString();
    }

    private static IEnumerable<XElement> GetStepElements(XDocument doc)
    {
        var root = doc.Root;
        if (root == null) return Enumerable.Empty<XElement>();

        // Mac-XMSC: steps inside <Script> wrapper
        var script = root.Element("Script");
        if (script != null)
            return script.Elements("Step");

        // Mac-XMSS: steps directly under root
        return root.Elements("Step");
    }

    private static StepDefinition LookupStep(XElement step)
    {
        var name = step.Attribute("name")?.Value;
        if (name != null && StepCatalogLoader.ByName.TryGetValue(name, out var byName))
            return byName;

        var idStr = step.Attribute("id")?.Value;
        if (idStr != null && int.TryParse(idStr, out var id) &&
            StepCatalogLoader.ById.TryGetValue(id, out var byId))
            return byId;

        // Fallback: create a minimal definition for unknown steps
        return new StepDefinition
        {
            Name = name ?? "Unknown",
            Id = 0,
            SelfClosing = !step.HasElements
        };
    }
}
