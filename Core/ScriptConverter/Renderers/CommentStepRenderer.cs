using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.Renderers;

public class CommentStepRenderer : IMultiStepRenderer
{
    public string[] StepNames => ["# (comment)"];

    public string ToHr(XElement step, StepDefinition definition)
    {
        var text = step.Element("Text")?.Value ?? "";
        if (text.Contains('\n'))
        {
            // Prefix each line with # so the grammar highlights them all
            var lines = text.Split('\n');
            return string.Join("\n", lines.Select(l => $"# {l.TrimEnd('\r')}"));
        }
        return $"# {text}";
    }

    public string ToXml(ParsedLine line, StepDefinition definition)
    {
        var enable = line.Disabled ? "False" : "True";
        var text = line.Params.Length > 0 ? line.Params[0] : "";
        return $"<Step enable=\"{enable}\" id=\"89\" name=\"# (comment)\"><Text>{GenericStepRenderer.XmlEscape(text)}</Text></Step>";
    }
}
