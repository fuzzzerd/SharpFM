using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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

    public string ContentType { get; set; } = "Text";
    public Calculation? Calculation { get; set; }
    public string Text { get; set; } = "";
    public SendEventTarget Event { get; set; } = SendEventTarget.Default();

    private SendEventStep() : base(false) { }

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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SendEventStep>(step, Metadata);

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
        // Canonical: ContentType, optional Calculation, optional Text, then Event.
        Shape =
        [
            new EnumValueChild("ContentType") { PocoProperty = "ContentType", DefaultValue = "Text", Display = DisplayMode.Hidden },
            new BareCalcChild { PocoProperty = "Calculation", Optional = true, Display = DisplayMode.Native },
            new NamedTextChild("Text") { PocoProperty = "Text", Optional = true, Display = DisplayMode.Native },
            new ValueTypeChild("Event") { PocoProperty = "Event", Display = DisplayMode.Hidden },
        ],
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
