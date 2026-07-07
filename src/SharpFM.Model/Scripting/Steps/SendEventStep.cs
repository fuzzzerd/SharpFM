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

    /// <summary>
    /// Display edits are anchor-preserved when the event carries behaviour
    /// state the display line cannot express: the copy/wait/foreground flags
    /// and the target type never appear in display text, so any non-baseline
    /// value seals the step.
    /// </summary>
    public override bool IsFullyEditable =>
        Event is { CopyResultToClipboard: false, WaitForCompletion: false, BringTargetToForeground: false, TargetType: "" };

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
        // Display shape is positional: [ targetName ; class ; id ; content ].
        // The event's behaviour flags and target type are not displayable —
        // instances carrying them are sealed (see IsFullyEditable).
        var tokens = Array.ConvertAll(hrParams, h => h.Trim());
        var targetName = tokens.Length >= 1 ? tokens[0] : "";
        var cls = tokens.Length >= 2 ? tokens[1] : "";
        var id = tokens.Length >= 3 ? tokens[2] : "";
        var content = tokens.Length >= 4 ? tokens[3] : "";

        string contentType;
        Calculation? calculation = null;
        string text = "";
        if (content == "<file>")
            contentType = "File";
        else if (content.Length >= 2 && content.StartsWith("\"", StringComparison.Ordinal) && content.EndsWith("\"", StringComparison.Ordinal))
        {
            contentType = "Text";
            text = content.Substring(1, content.Length - 2);
        }
        else if (content.Length > 0)
        {
            contentType = "Calculation";
            calculation = new Calculation(content);
        }
        else
            contentType = "Text";

        var evt = new SendEventTarget(false, false, false, "", targetName, id, cls);
        return new SendEventStep(contentType, calculation, text, evt, enabled);
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
            new HrOnly("ContentType") { DisplayValues = ["Text", "File", "Calculation"] },
            new BareCalcChild { PocoProperty = "Calculation", Optional = true, Display = DisplayMode.Native },
            new NamedTextChild("Text") { PocoProperty = "Text", Optional = true, Display = DisplayMode.Native },
            new ValueTypeChild("Event") { PocoProperty = "Event", Display = DisplayMode.Hidden },
            new HrOnly("Event"),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
