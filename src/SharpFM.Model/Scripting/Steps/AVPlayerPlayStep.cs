using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class AVPlayerPlayStep : ScriptStep, IStepFactory
{
    public const int XmlId = 177;
    public const string XmlName = "AVPlayer Play";

    public string Source { get; set; }
    public Calculation Repetition { get; set; }
    public string Presentation { get; set; }
    public Calculation Position { get; set; }
    public Calculation StartOffset { get; set; }
    public Calculation EndOffset { get; set; }
    public bool HideControls { get; set; }
    public bool DisableInteraction { get; set; }

    public AVPlayerPlayStep(
        string source = "Object Name",
        Calculation? repetition = null,
        string presentation = "Start Full Screen",
        Calculation? position = null,
        Calculation? startOffset = null,
        Calculation? endOffset = null,
        bool hideControls = false,
        bool disableInteraction = false,
        bool enabled = true)
        : base(enabled)
    {
        Source = source;
        Repetition = repetition ?? new Calculation("");
        Presentation = presentation;
        Position = position ?? new Calculation("");
        StartOffset = startOffset ?? new Calculation("");
        EndOffset = endOffset ?? new Calculation("");
        HideControls = hideControls;
        DisableInteraction = disableInteraction;
    }

    private static readonly IReadOnlyDictionary<string, string> _SourceToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["Object Name"] = "Object Name",
        ["Field"] = "Field",
        ["URL"] = "URL",
    };
    private static readonly IReadOnlyDictionary<string, string> _SourceFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["Object Name"] = "Object Name",
        ["Field"] = "Field",
        ["URL"] = "URL",
    };
    private static string SourceHr(string x) => _SourceToHr.TryGetValue(x, out var h) ? h : x;
    private static string SourceXml(string h) => _SourceFromHr.TryGetValue(h, out var x) ? x : h;

    private static readonly IReadOnlyDictionary<string, string> _PresentationToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["Start Full Screen"] = "Start Full Screen",
        ["Full Screen Only"] = "Full Screen Only",
        ["Start Embedded"] = "Start Embedded",
        ["Embedded Only"] = "Embedded Only",
        ["Audio Only"] = "Audio Only",
    };
    private static readonly IReadOnlyDictionary<string, string> _PresentationFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["Start Full Screen"] = "Start Full Screen",
        ["Full Screen Only"] = "Full Screen Only",
        ["Start Embedded"] = "Start Embedded",
        ["Embedded Only"] = "Embedded Only",
        ["Audio Only"] = "Audio Only",
    };
    private static string PresentationHr(string x) => _PresentationToHr.TryGetValue(x, out var h) ? h : x;
    private static string PresentationXml(string h) => _PresentationFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Source", new XAttribute("value", Source)),
            new XElement("Repetition", Repetition.ToXml("Calculation")),
            new XElement("Presentation", new XAttribute("value", Presentation)),
            new XElement("PlaybackPosition", Position.ToXml("Calculation")),
            new XElement("StartOffset", StartOffset.ToXml("Calculation")),
            new XElement("EndOffset", EndOffset.ToXml("Calculation")),
            new XElement("HideControls", new XAttribute("value", HideControls ? "True" : "False")),
            new XElement("DisableInteraction", new XAttribute("value", DisableInteraction ? "True" : "False")));

    public override string ToDisplayLine() =>
        "AVPlayer Play [ " + SourceHr(Source) + " ; " + "Repetition: " + Repetition.Text + " ; " + "Presentation: " + PresentationHr(Presentation) + " ; " + "Position: " + Position.Text + " ; " + "Start Offset: " + StartOffset.Text + " ; " + "End Offset: " + EndOffset.Text + " ; " + "Hide Controls: " + (HideControls ? "On" : "Off") + " ; " + "Disable Interaction: " + (DisableInteraction ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var source_v = step.Element("Source")?.Attribute("value")?.Value ?? "Object Name";
        var repetition_vWrapEl = step.Element("Repetition");
        var repetition_vCalcEl = repetition_vWrapEl?.Element("Calculation");
        var repetition_v = repetition_vCalcEl is not null ? Calculation.FromXml(repetition_vCalcEl) : new Calculation("");
        var presentation_v = step.Element("Presentation")?.Attribute("value")?.Value ?? "Start Full Screen";
        var position_vWrapEl = step.Element("PlaybackPosition");
        var position_vCalcEl = position_vWrapEl?.Element("Calculation");
        var position_v = position_vCalcEl is not null ? Calculation.FromXml(position_vCalcEl) : new Calculation("");
        var startOffset_vWrapEl = step.Element("StartOffset");
        var startOffset_vCalcEl = startOffset_vWrapEl?.Element("Calculation");
        var startOffset_v = startOffset_vCalcEl is not null ? Calculation.FromXml(startOffset_vCalcEl) : new Calculation("");
        var endOffset_vWrapEl = step.Element("EndOffset");
        var endOffset_vCalcEl = endOffset_vWrapEl?.Element("Calculation");
        var endOffset_v = endOffset_vCalcEl is not null ? Calculation.FromXml(endOffset_vCalcEl) : new Calculation("");
        var hideControls_v = step.Element("HideControls")?.Attribute("value")?.Value == "True";
        var disableInteraction_v = step.Element("DisableInteraction")?.Attribute("value")?.Value == "True";
        return new AVPlayerPlayStep(source_v, repetition_v, presentation_v, position_v, startOffset_v, endOffset_v, hideControls_v, disableInteraction_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string source_v = "Object Name";
        Calculation? repetition_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Repetition:", StringComparison.OrdinalIgnoreCase)) { repetition_v = new Calculation(tok.Substring(11).Trim()); break; } }
        string presentation_v = "Start Full Screen";
        foreach (var tok in tokens) { if (tok.StartsWith("Presentation:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); presentation_v = PresentationXml(v); break; } }
        Calculation? position_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Position:", StringComparison.OrdinalIgnoreCase)) { position_v = new Calculation(tok.Substring(9).Trim()); break; } }
        Calculation? startOffset_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Start Offset:", StringComparison.OrdinalIgnoreCase)) { startOffset_v = new Calculation(tok.Substring(13).Trim()); break; } }
        Calculation? endOffset_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("End Offset:", StringComparison.OrdinalIgnoreCase)) { endOffset_v = new Calculation(tok.Substring(11).Trim()); break; } }
        bool hideControls_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Hide Controls:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(14).Trim(); hideControls_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool disableInteraction_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Disable Interaction:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(20).Trim(); disableInteraction_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        return new AVPlayerPlayStep(source_v, repetition_v, presentation_v, position_v, startOffset_v, endOffset_v, hideControls_v, disableInteraction_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/avplayer-play.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Source",
                XmlElement = "Source",
                Type = "enum",
                XmlAttr = "value",
                ValidValues = ["Object Name", "Field", "URL"],
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Repetition",
            },
            new ParamMetadata
            {
                Name = "Presentation",
                XmlElement = "Presentation",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Presentation",
                ValidValues = ["Start Full Screen", "Full Screen Only", "Start Embedded", "Embedded Only", "Audio Only"],
                DefaultValue = "Start Full Screen",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Position",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Start Offset",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "End Offset",
            },
            new ParamMetadata
            {
                Name = "HideControls",
                XmlElement = "HideControls",
                Type = "boolean",
                XmlAttr = "value",
                HrLabel = "Hide Controls",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "DisableInteraction",
                XmlElement = "DisableInteraction",
                Type = "boolean",
                XmlAttr = "value",
                HrLabel = "Disable Interaction",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
