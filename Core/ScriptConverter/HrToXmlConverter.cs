using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter;

public record ConversionResult(string Xml, List<string> Errors);

public static class HrToXmlConverter
{
    public static ConversionResult Convert(string hrText)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(hrText))
            return new ConversionResult(PrettyPrint(WrapSnippet("")), errors);

        var lines = ScriptLineParser.Parse(hrText);
        var mergedLines = MergeCommentContinuations(lines);
        var sb = new StringBuilder();

        for (int i = 0; i < mergedLines.Count; i++)
        {
            var line = mergedLines[i];
            try
            {
                var stepXml = ConvertLine(line);
                sb.Append(stepXml);
            }
            catch (Exception ex)
            {
                errors.Add($"Line {i + 1}: {ex.Message}");
                // Fallback: emit as comment preserving original text
                var escaped = GenericStepRenderer.XmlEscape(line.RawLine.Trim());
                sb.Append($"<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>{escaped}</Text></Step>");
            }
        }

        return new ConversionResult(PrettyPrint(WrapSnippet(sb.ToString())), errors);
    }

    private static string ConvertLine(ParsedLine line)
    {
        if (line.IsComment)
        {
            var renderer = StepRendererRegistry.GetRenderer("# (comment)");
            var def = StepCatalogLoader.ByName["# (comment)"];
            return renderer.ToXml(line, def);
        }

        if (StepCatalogLoader.ByName.TryGetValue(line.StepName, out var definition))
        {
            var renderer = StepRendererRegistry.GetRenderer(line.StepName);
            return renderer.ToXml(line, definition);
        }

        // Unknown bare text → new comment step
        var text = GenericStepRenderer.XmlEscape(line.RawLine.Trim());
        return $"<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>{text}</Text></Step>";
    }

    private static bool IsBareText(ParsedLine line)
    {
        return !line.IsComment
            && line.Params.Length == 0
            && !StepCatalogLoader.ByName.ContainsKey(line.StepName);
    }

    private static List<ParsedLine> MergeCommentContinuations(List<ParsedLine> lines)
    {
        var result = new List<ParsedLine>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            bool shouldMerge = result.Count > 0 && result[^1].IsComment
                && (IsBareText(line) || line.IsComment);

            if (shouldMerge)
            {
                var prev = result[^1];
                var prevText = prev.Params.Length > 0 ? prev.Params[0] : "";
                var thisText = line.IsComment && line.Params.Length > 0
                    ? line.Params[0]
                    : line.RawLine.Trim();
                var mergedText = string.IsNullOrEmpty(prevText)
                    ? thisText
                    : prevText + "\n" + thisText;
                result[^1] = new ParsedLine(
                    prev.StepName,
                    new[] { mergedText },
                    prev.Disabled,
                    true,
                    prev.RawLine + "\n" + line.RawLine);
            }
            else
            {
                result.Add(line);
            }
        }

        return result;
    }

    private static string WrapSnippet(string stepsXml)
    {
        return $"<fmxmlsnippet type=\"FMObjectList\">{stepsXml}</fmxmlsnippet>";
    }

    private static string PrettyPrint(string xml)
    {
        try
        {
            var element = XElement.Parse(xml);
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineOnAttributes = false
            };
            using (var writer = XmlWriter.Create(sb, settings))
            {
                element.Save(writer);
            }
            return sb.ToString();
        }
        catch
        {
            return xml;
        }
    }
}
