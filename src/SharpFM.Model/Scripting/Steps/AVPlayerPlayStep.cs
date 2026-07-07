using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class AVPlayerPlayStep : ScriptStep, IStepFactory
{
    public const int XmlId = 177;
    public const string XmlName = "AVPlayer Play";

    public string Source { get; set; } = "Object Name";
    public Calculation? Repetition { get; set; }
    public string Presentation { get; set; } = "Start Full Screen";
    public Calculation? Position { get; set; }
    public Calculation? StartOffset { get; set; }
    public Calculation? EndOffset { get; set; }
    // HideControls/DisableInteraction emit <El value="True|False"/>. They are
    // string-backed (not bool) so the unconfigured canonical form, which omits
    // them, round-trips: a blank value is dropped by the Optional EnumValueChild.
    public string HideControls { get; set; } = "";
    public string DisableInteraction { get; set; } = "";

    private AVPlayerPlayStep() : base(false) { }

    public AVPlayerPlayStep(
        string source = "Object Name",
        Calculation? repetition = null,
        string presentation = "Start Full Screen",
        Calculation? position = null,
        Calculation? startOffset = null,
        Calculation? endOffset = null,
        string hideControls = "",
        string disableInteraction = "",
        bool enabled = true)
        : base(enabled)
    {
        Source = source;
        Repetition = repetition;
        Presentation = presentation;
        Position = position;
        StartOffset = startOffset;
        EndOffset = endOffset;
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "AVPlayer Play [ " + SourceHr(Source) + " ; " + "Repetition: " + (Repetition?.Text ?? "") + " ; " + "Presentation: " + PresentationHr(Presentation) + " ; " + "Position: " + (Position?.Text ?? "") + " ; " + "Start Offset: " + (StartOffset?.Text ?? "") + " ; " + "End Offset: " + (EndOffset?.Text ?? "") + " ; " + "Hide Controls: " + (HideControls == "True" ? "On" : "Off") + " ; " + "Disable Interaction: " + (DisableInteraction == "True" ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<AVPlayerPlayStep>(step, Metadata);

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
        string hideControls_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Hide Controls:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(14).Trim(); hideControls_v = v.Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : "False"; break; } }
        string disableInteraction_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Disable Interaction:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(20).Trim(); disableInteraction_v = v.Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : "False"; break; } }
        return new AVPlayerPlayStep(source_v, repetition_v, presentation_v, position_v, startOffset_v, endOffset_v, hideControls_v, disableInteraction_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/avplayer-play.html",
        // Canonical form carries only Source; every other option element is
        // omitted until configured, so all but Source are Optional. HideControls
        // and DisableInteraction use a value="True|False" attribute and are
        // modeled as EnumValueChild over a string so they can be dropped.
        Shape =
        [
            new EnumValueChild("Source") { PocoProperty = "Source", ValidValues = ["Object Name", "Field", "URL"] },
            new NamedCalcChild("Repetition") { PocoProperty = "Repetition", HrLabel = "Repetition", Optional = true },
            new EnumValueChild("Presentation") { PocoProperty = "Presentation", HrLabel = "Presentation", Optional = true, DisplayValues = ["Start Full Screen", "Full Screen Only", "Start Embedded", "Embedded Only", "Audio Only"] },
            new NamedCalcChild("PlaybackPosition") { PocoProperty = "Position", HrLabel = "Position", Optional = true },
            new NamedCalcChild("StartOffset") { PocoProperty = "StartOffset", HrLabel = "Start Offset", Optional = true },
            new NamedCalcChild("EndOffset") { PocoProperty = "EndOffset", HrLabel = "End Offset", Optional = true },
            new EnumValueChild("HideControls") { PocoProperty = "HideControls", HrLabel = "Hide Controls", Optional = true, DisplayValues = ["On", "Off"] },
            new EnumValueChild("DisableInteraction") { PocoProperty = "DisableInteraction", HrLabel = "Disable Interaction", Optional = true, DisplayValues = ["On", "Off"] },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
