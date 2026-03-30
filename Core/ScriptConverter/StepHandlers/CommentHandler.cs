using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.StepHandlers;

internal class CommentHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["# (comment)"];

    public string? ToDisplayLine(ScriptStep step)
    {
        var text = step.ParamValues.FirstOrDefault(p => p.Definition.XmlElement == "Text")?.Value ?? "";
        if (text.Contains('\n'))
        {
            var lines = text.Split('\n');
            return string.Join("\n", lines.Select(l => $"# {l.TrimEnd('\r')}"));
        }
        return $"# {text}";
    }

    public XElement? ToXml(ScriptStep step)
    {
        var text = step.ParamValues.FirstOrDefault(p => p.Definition.XmlElement == "Text")?.Value ?? "";
        var el = MakeStep(89, "# (comment)", step.Enabled);
        el.Add(new XElement("Text", text));
        return el;
    }

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
    {
        return null; // Comments are handled by ScriptStep.FromDisplayLine directly
    }
}
