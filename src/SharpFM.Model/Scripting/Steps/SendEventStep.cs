using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Send Event [ targetName ; class ; id ; content ]. The step has three
/// content modes driven by <c>&lt;ContentType&gt;</c>:
/// <list type="bullet">
/// <item><c>Text</c> — literal script text held in the <c>&lt;Text&gt;</c> element.</item>
/// <item><c>File</c> — launches a file/app; the <c>&lt;Text&gt;</c> element is self-closing.</item>
/// <item><c>Calculation</c> — the script text is computed from <c>&lt;Calculation&gt;</c>.</item>
/// </list>
/// </summary>
public sealed class SendEventStep : ScriptStep, IStepFactory
{
    public const int XmlId = 57;
    public const string XmlName = "Send Event";

    public string ContentType { get; set; }
    public Calculation? Calculation { get; set; }
    public string Text { get; set; }
    public SendEventTarget Event { get; set; }

    public SendEventStep(
        string contentType = "Text",
        Calculation? calculation = null,
        string text = "",
        SendEventTarget? evt = null,
        bool enabled = true)
        : base(enabled)
    {
        ContentType = contentType;
        Calculation = calculation;
        Text = text;
        Event = evt ?? SendEventTarget.Default();
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("ContentType", new XAttribute("value", ContentType)));
        if (Calculation is not null) step.Add(Calculation.ToXml("Calculation"));
        step.Add(string.IsNullOrEmpty(Text) ? new XElement("Text") : new XElement("Text", Text));
        step.Add(Event.ToXml());
        return step;
    }

    public override string ToDisplayLine()
    {
        var content = ContentType switch
        {
            "Calculation" => Calculation?.Text ?? "",
            "Text" => string.IsNullOrEmpty(Text) ? "" : $"\"{Text}\"",
            "File" => "<file>",
            _ => ContentType,
        };
        return $"Send Event [ {Event.TargetName} ; {Event.Class} ; {Event.Id} ; {content} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var contentType = step.Element("ContentType")?.Attribute("value")?.Value ?? "Text";
        var calcEl = step.Element("Calculation");
        var calc = calcEl is not null ? Values.Calculation.FromXml(calcEl) : null;
        var text = step.Element("Text")?.Value ?? "";
        var eventEl = step.Element("Event");
        var evt = eventEl is not null ? SendEventTarget.FromXml(eventEl) : SendEventTarget.Default();
        return new SendEventStep(contentType, calc, text, evt, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Display form is lossy for Send Event (event attrs can't all be
        // round-tripped through display). Best-effort parse; full fidelity
        // is only via XML round-trip.
        return new SendEventStep(enabled: enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/send-event.html",
        Params =
        [
            new ParamMetadata { Name = "ContentType", XmlElement = "ContentType", XmlAttr = "value", Type = "enum", ValidValues = ["Text", "File", "Calculation"], DefaultValue = "Text", Required = true },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation" },
            new ParamMetadata { Name = "Text", XmlElement = "Text", Type = "text" },
            new ParamMetadata { Name = "Event", XmlElement = "Event", Type = "complex", Required = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
