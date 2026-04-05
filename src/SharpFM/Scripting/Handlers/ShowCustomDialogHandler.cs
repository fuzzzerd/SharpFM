using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

internal class ShowCustomDialogHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Show Custom Dialog"];

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
    {
        // Support both labeled (Title: x ; Message: y) and positional ("title" ; "message") formats
        var title = ExtractLabeled(hrParams, "Title");
        var message = ExtractLabeled(hrParams, "Message");
        var buttonsRaw = ExtractLabeled(hrParams, "Buttons");

        // Fall back to positional: first param = title, second = message
        if (title is null && message is null && hrParams.Length >= 1)
        {
            title = hrParams[0].Trim();
            if (hrParams.Length >= 2 && !hrParams[1].Trim().StartsWith("Buttons:", StringComparison.OrdinalIgnoreCase))
                message = hrParams[1].Trim();
            if (hrParams.Length >= 3)
                buttonsRaw ??= ExtractLabeled(hrParams, "Buttons") ?? hrParams[2].Trim();
        }

        title ??= "";
        message ??= "";
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
