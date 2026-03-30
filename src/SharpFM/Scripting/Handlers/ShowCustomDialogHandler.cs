using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

internal class ShowCustomDialogHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Show Custom Dialog"];

    public string? ToDisplayLine(ScriptStep step)
    {
        var title = step.SourceXml?.Element("Title")?.Element("Calculation")?.Value;
        var message = step.SourceXml?.Element("Message")?.Element("Calculation")?.Value;
        var buttons = step.SourceXml?.Element("Buttons")?.Elements("Button")
            .Select(b => b.Element("Calculation")?.Value)
            .Where(b => !string.IsNullOrEmpty(b))
            .ToList() ?? new List<string?>();

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(title)) parts.Add($"Title: {title}");
        if (!string.IsNullOrEmpty(message)) parts.Add($"Message: {message}");
        if (buttons.Count > 0) parts.Add($"Buttons: {string.Join(", ", buttons)}");

        return parts.Count == 0
            ? "Show Custom Dialog"
            : $"Show Custom Dialog [ {string.Join(" ; ", parts)} ]";
    }

    public XElement? ToXml(ScriptStep step)
    {
        return BuildXmlFromDisplay(step.Definition!, step.Enabled,
            ScriptLineParser.ParseRaw(step.ToDisplayLine()).Params);
    }

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
    {
        var title = ExtractLabeled(hrParams, "Title") ?? "";
        var message = ExtractLabeled(hrParams, "Message") ?? "";
        var buttonsRaw = ExtractLabeled(hrParams, "Buttons");
        var buttons = buttonsRaw?.Split(',').Select(b => b.Trim()).ToList() ?? new List<string>();

        var step = MakeStep(87, "Show Custom Dialog", enabled);
        step.Add(XElement.Parse($"<Title><Calculation><![CDATA[{title}]]></Calculation></Title>"));
        step.Add(XElement.Parse($"<Message><Calculation><![CDATA[{message}]]></Calculation></Message>"));

        if (buttons.Count > 0)
        {
            var buttonsEl = new XElement("Buttons");
            foreach (var btn in buttons)
                buttonsEl.Add(XElement.Parse($"<Button CommitState=\"True\"><Calculation><![CDATA[{btn}]]></Calculation></Button>"));
            step.Add(buttonsEl);
        }
        return step;
    }
}
