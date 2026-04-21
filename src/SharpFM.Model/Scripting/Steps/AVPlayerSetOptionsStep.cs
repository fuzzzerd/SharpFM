using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class AVPlayerSetOptionsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 179;
    public const string XmlName = "AVPlayer Set Options";

    public string Presentation { get; set; }
    public bool DisableInteraction { get; set; }
    public bool HideControls { get; set; }
    public bool DisableExternalControls { get; set; }
    public bool PauseInBackground { get; set; }
    public Calculation Position { get; set; }
    public Calculation StartOffset { get; set; }
    public Calculation EndOffset { get; set; }
    public Calculation Volume { get; set; }
    public string Zoom { get; set; }
    public string Sequence { get; set; }

    public AVPlayerSetOptionsStep(
        string presentation = "Start Full Screen",
        bool disableInteraction = false,
        bool hideControls = false,
        bool disableExternalControls = false,
        bool pauseInBackground = false,
        Calculation? position = null,
        Calculation? startOffset = null,
        Calculation? endOffset = null,
        Calculation? volume = null,
        string zoom = "Fit",
        string sequence = "None",
        bool enabled = true)
        : base(null, enabled)
    {
        Presentation = presentation;
        DisableInteraction = disableInteraction;
        HideControls = hideControls;
        DisableExternalControls = disableExternalControls;
        PauseInBackground = pauseInBackground;
        Position = position ?? new Calculation("");
        StartOffset = startOffset ?? new Calculation("");
        EndOffset = endOffset ?? new Calculation("");
        Volume = volume ?? new Calculation("");
        Zoom = zoom;
        Sequence = sequence;
    }

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

    private static readonly IReadOnlyDictionary<string, string> _ZoomToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["Fit"] = "Fit",
        ["Fill"] = "Fill",
        ["Stretch"] = "Stretch",
        ["Fit Only"] = "Fit Only",
        ["Fill Only"] = "Fill Only",
        ["Stretch Only"] = "Stretch Only",
    };
    private static readonly IReadOnlyDictionary<string, string> _ZoomFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["Fit"] = "Fit",
        ["Fill"] = "Fill",
        ["Stretch"] = "Stretch",
        ["Fit Only"] = "Fit Only",
        ["Fill Only"] = "Fill Only",
        ["Stretch Only"] = "Stretch Only",
    };
    private static string ZoomHr(string x) => _ZoomToHr.TryGetValue(x, out var h) ? h : x;
    private static string ZoomXml(string h) => _ZoomFromHr.TryGetValue(h, out var x) ? x : h;

    private static readonly IReadOnlyDictionary<string, string> _SequenceToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["None"] = "None",
        ["Next"] = "Next",
        ["Previous"] = "Previous",
    };
    private static readonly IReadOnlyDictionary<string, string> _SequenceFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["None"] = "None",
        ["Next"] = "Next",
        ["Previous"] = "Previous",
    };
    private static string SequenceHr(string x) => _SequenceToHr.TryGetValue(x, out var h) ? h : x;
    private static string SequenceXml(string h) => _SequenceFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Presentation", new XAttribute("value", Presentation)),
            new XElement("DisableInteraction", new XAttribute("value", DisableInteraction ? "True" : "False")),
            new XElement("HideControls", new XAttribute("value", HideControls ? "True" : "False")),
            new XElement("DisableExternalControls", new XAttribute("value", DisableExternalControls ? "True" : "False")),
            new XElement("PauseInBackground", new XAttribute("value", PauseInBackground ? "True" : "False")),
            new XElement("PlaybackPosition", Position.ToXml("Calculation")),
            new XElement("StartOffset", StartOffset.ToXml("Calculation")),
            new XElement("EndOffset", EndOffset.ToXml("Calculation")),
            new XElement("Volume", Volume.ToXml("Calculation")),
            new XElement("Zoom", new XAttribute("value", Zoom)),
            new XElement("Sequence", new XAttribute("value", Sequence)));

    public override string ToDisplayLine() =>
        "AVPlayer Set Options [ " + "Presentation: " + PresentationHr(Presentation) + " ; " + "Disable Interaction: " + (DisableInteraction ? "On" : "Off") + " ; " + "Hide Controls: " + (HideControls ? "On" : "Off") + " ; " + "Disable External Controls: " + (DisableExternalControls ? "On" : "Off") + " ; " + "Pause in Background: " + (PauseInBackground ? "On" : "Off") + " ; " + "Position: " + Position.Text + " ; " + "Start Offset: " + StartOffset.Text + " ; " + "End Offset: " + EndOffset.Text + " ; " + "Volume: " + Volume.Text + " ; " + "Zoom: " + ZoomHr(Zoom) + " ; " + "Sequence: " + SequenceHr(Sequence) + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var presentation_v = step.Element("Presentation")?.Attribute("value")?.Value ?? "Start Full Screen";
        var disableInteraction_v = step.Element("DisableInteraction")?.Attribute("value")?.Value == "True";
        var hideControls_v = step.Element("HideControls")?.Attribute("value")?.Value == "True";
        var disableExternalControls_v = step.Element("DisableExternalControls")?.Attribute("value")?.Value == "True";
        var pauseInBackground_v = step.Element("PauseInBackground")?.Attribute("value")?.Value == "True";
        var position_vWrapEl = step.Element("PlaybackPosition");
        var position_vCalcEl = position_vWrapEl?.Element("Calculation");
        var position_v = position_vCalcEl is not null ? Calculation.FromXml(position_vCalcEl) : new Calculation("");
        var startOffset_vWrapEl = step.Element("StartOffset");
        var startOffset_vCalcEl = startOffset_vWrapEl?.Element("Calculation");
        var startOffset_v = startOffset_vCalcEl is not null ? Calculation.FromXml(startOffset_vCalcEl) : new Calculation("");
        var endOffset_vWrapEl = step.Element("EndOffset");
        var endOffset_vCalcEl = endOffset_vWrapEl?.Element("Calculation");
        var endOffset_v = endOffset_vCalcEl is not null ? Calculation.FromXml(endOffset_vCalcEl) : new Calculation("");
        var volume_vWrapEl = step.Element("Volume");
        var volume_vCalcEl = volume_vWrapEl?.Element("Calculation");
        var volume_v = volume_vCalcEl is not null ? Calculation.FromXml(volume_vCalcEl) : new Calculation("");
        var zoom_v = step.Element("Zoom")?.Attribute("value")?.Value ?? "Fit";
        var sequence_v = step.Element("Sequence")?.Attribute("value")?.Value ?? "None";
        return new AVPlayerSetOptionsStep(presentation_v, disableInteraction_v, hideControls_v, disableExternalControls_v, pauseInBackground_v, position_v, startOffset_v, endOffset_v, volume_v, zoom_v, sequence_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string presentation_v = "Start Full Screen";
        foreach (var tok in tokens) { if (tok.StartsWith("Presentation:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); presentation_v = PresentationXml(v); break; } }
        bool disableInteraction_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Disable Interaction:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(20).Trim(); disableInteraction_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool hideControls_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Hide Controls:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(14).Trim(); hideControls_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool disableExternalControls_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Disable External Controls:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(26).Trim(); disableExternalControls_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool pauseInBackground_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Pause in Background:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(20).Trim(); pauseInBackground_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? position_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Position:", StringComparison.OrdinalIgnoreCase)) { position_v = new Calculation(tok.Substring(9).Trim()); break; } }
        Calculation? startOffset_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Start Offset:", StringComparison.OrdinalIgnoreCase)) { startOffset_v = new Calculation(tok.Substring(13).Trim()); break; } }
        Calculation? endOffset_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("End Offset:", StringComparison.OrdinalIgnoreCase)) { endOffset_v = new Calculation(tok.Substring(11).Trim()); break; } }
        Calculation? volume_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Volume:", StringComparison.OrdinalIgnoreCase)) { volume_v = new Calculation(tok.Substring(7).Trim()); break; } }
        string zoom_v = "Fit";
        foreach (var tok in tokens) { if (tok.StartsWith("Zoom:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(5).Trim(); zoom_v = ZoomXml(v); break; } }
        string sequence_v = "None";
        foreach (var tok in tokens) { if (tok.StartsWith("Sequence:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(9).Trim(); sequence_v = SequenceXml(v); break; } }
        return new AVPlayerSetOptionsStep(presentation_v, disableInteraction_v, hideControls_v, disableExternalControls_v, pauseInBackground_v, position_v, startOffset_v, endOffset_v, volume_v, zoom_v, sequence_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/avplayer-set-options.html",
        Params =
        [
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
                Name = "DisableInteraction",
                XmlElement = "DisableInteraction",
                Type = "boolean",
                XmlAttr = "value",
                HrLabel = "Disable Interaction",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
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
                Name = "DisableExternalControls",
                XmlElement = "DisableExternalControls",
                Type = "boolean",
                XmlAttr = "value",
                HrLabel = "Disable External Controls",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "PauseInBackground",
                XmlElement = "PauseInBackground",
                Type = "boolean",
                XmlAttr = "value",
                HrLabel = "Pause in Background",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
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
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Volume",
            },
            new ParamMetadata
            {
                Name = "Zoom",
                XmlElement = "Zoom",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Zoom",
                ValidValues = ["Fit", "Fill", "Stretch", "Fit Only", "Fill Only", "Stretch Only"],
                DefaultValue = "Fit",
            },
            new ParamMetadata
            {
                Name = "Sequence",
                XmlElement = "Sequence",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Sequence",
                ValidValues = ["None", "Next", "Previous"],
                DefaultValue = "None",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
