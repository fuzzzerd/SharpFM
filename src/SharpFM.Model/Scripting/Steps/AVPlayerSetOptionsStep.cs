using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class AVPlayerSetOptionsStep : ScriptStep<AVPlayerSetOptionsStep>, IStepFactory
{
    public const int XmlId = 179;
    public const string XmlName = "AVPlayer Set Options";

    public string Presentation { get; set; } = "Start Full Screen";
    // The four toggle options emit <El value="True|False"/>. They are
    // string-backed (not bool) so the unconfigured canonical form, which omits
    // them entirely, round-trips: a blank value is dropped by Optional EnumValueChild.
    public string DisableInteraction { get; set; } = "";
    public string HideControls { get; set; } = "";
    public string DisableExternalControls { get; set; } = "";
    public string PauseInBackground { get; set; } = "";
    public Calculation? Position { get; set; }
    public Calculation? StartOffset { get; set; }
    public Calculation? EndOffset { get; set; }
    public Calculation? Volume { get; set; }
    public string Zoom { get; set; } = "Fit";
    public string Sequence { get; set; } = "None";

    private AVPlayerSetOptionsStep() : base(false) { }

    public AVPlayerSetOptionsStep(
        string presentation = "Start Full Screen",
        string disableInteraction = "",
        string hideControls = "",
        string disableExternalControls = "",
        string pauseInBackground = "",
        Calculation? position = null,
        Calculation? startOffset = null,
        Calculation? endOffset = null,
        Calculation? volume = null,
        string zoom = "Fit",
        string sequence = "None",
        bool enabled = true)
        : base(enabled)
    {
        Presentation = presentation;
        DisableInteraction = disableInteraction;
        HideControls = hideControls;
        DisableExternalControls = disableExternalControls;
        PauseInBackground = pauseInBackground;
        Position = position;
        StartOffset = startOffset;
        EndOffset = endOffset;
        Volume = volume;
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

    // Hand-written: option toggles map the wire's "True"/absent values to
    // On/Off, a translation display metadata cannot express.
    public override string ToDisplayLine() =>
        "AVPlayer Set Options [ " + "Presentation: " + PresentationHr(Presentation) + " ; " + "Disable Interaction: " + (DisableInteraction == "True" ? "On" : "Off") + " ; " + "Hide Controls: " + (HideControls == "True" ? "On" : "Off") + " ; " + "Disable External Controls: " + (DisableExternalControls == "True" ? "On" : "Off") + " ; " + "Pause in Background: " + (PauseInBackground == "True" ? "On" : "Off") + " ; " + "Position: " + (Position?.Text ?? "") + " ; " + "Start Offset: " + (StartOffset?.Text ?? "") + " ; " + "End Offset: " + (EndOffset?.Text ?? "") + " ; " + "Volume: " + (Volume?.Text ?? "") + " ; " + "Zoom: " + ZoomHr(Zoom) + " ; " + "Sequence: " + SequenceHr(Sequence) + " ]";

    /// <summary>
    /// Display edits are anchor-preserved when a toggle is explicitly stored
    /// as False: the display renders both the absent and the explicit-False
    /// forms as "Off", so a parsed "Off" maps to absent.
    /// </summary>
    public override bool IsFullyEditable =>
        DisableInteraction != "False" && HideControls != "False"
        && DisableExternalControls != "False" && PauseInBackground != "False";

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string presentation_v = "Start Full Screen";
        foreach (var tok in tokens) { if (tok.StartsWith("Presentation:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); presentation_v = PresentationXml(v); break; } }
        // "Off" is ambiguous between absent and explicit False; it maps to
        // absent (explicit-False instances are sealed).
        string disableInteraction_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Disable Interaction:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(20).Trim(); disableInteraction_v = v.Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : ""; break; } }
        string hideControls_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Hide Controls:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(14).Trim(); hideControls_v = v.Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : ""; break; } }
        string disableExternalControls_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Disable External Controls:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(26).Trim(); disableExternalControls_v = v.Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : ""; break; } }
        string pauseInBackground_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Pause in Background:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(20).Trim(); pauseInBackground_v = v.Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : ""; break; } }
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
        Presentation = presentation_v;
        DisableInteraction = disableInteraction_v;
        HideControls = hideControls_v;
        DisableExternalControls = disableExternalControls_v;
        PauseInBackground = pauseInBackground_v;
        Position = position_v;
        StartOffset = startOffset_v;
        EndOffset = endOffset_v;
        Volume = volume_v;
        Zoom = zoom_v;
        Sequence = sequence_v;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/avplayer-set-options.html",
        // Canonical unconfigured form is empty: every option is Optional. The four
        // toggle options use a value="True|False" attribute and are modeled as
        // EnumValueChild over a string so a blank (unconfigured) value is dropped.
        Shape =
        [
            new EnumValueChild("Presentation") { PocoProperty = "Presentation", HrLabel = "Presentation", Optional = true, DisplayValues = ["Start Full Screen", "Full Screen Only", "Start Embedded", "Embedded Only", "Audio Only"] },
            new EnumValueChild("DisableInteraction") { PocoProperty = "DisableInteraction", HrLabel = "Disable Interaction", Optional = true, DisplayValues = ["On", "Off"] },
            new EnumValueChild("HideControls") { PocoProperty = "HideControls", HrLabel = "Hide Controls", Optional = true, DisplayValues = ["On", "Off"] },
            new EnumValueChild("DisableExternalControls") { PocoProperty = "DisableExternalControls", HrLabel = "Disable External Controls", Optional = true, DisplayValues = ["On", "Off"] },
            new EnumValueChild("PauseInBackground") { PocoProperty = "PauseInBackground", HrLabel = "Pause in Background", Optional = true, DisplayValues = ["On", "Off"] },
            new NamedCalcChild("PlaybackPosition") { PocoProperty = "Position", HrLabel = "Position", Optional = true },
            new NamedCalcChild("StartOffset") { PocoProperty = "StartOffset", HrLabel = "Start Offset", Optional = true },
            new NamedCalcChild("EndOffset") { PocoProperty = "EndOffset", HrLabel = "End Offset", Optional = true },
            new NamedCalcChild("Volume") { PocoProperty = "Volume", HrLabel = "Volume", Optional = true },
            new EnumValueChild("Zoom") { PocoProperty = "Zoom", HrLabel = "Zoom", Optional = true, DisplayValues = ["Fit", "Fill", "Stretch", "Fit Only", "Fill Only", "Stretch Only"] },
            new EnumValueChild("Sequence") { PocoProperty = "Sequence", HrLabel = "Sequence", Optional = true, DisplayValues = ["None", "Next", "Previous"] },
        ],
    };
}
