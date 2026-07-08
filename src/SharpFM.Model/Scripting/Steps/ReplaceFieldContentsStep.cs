using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Replace Field Contents. Canonical form (skill): <c>NoInteract</c>,
/// <c>&lt;Restore state="False"/&gt;</c>, <c>&lt;With value="…"/&gt;</c>, then the
/// optional replacement payload (a <c>&lt;Calculation&gt;</c> or a
/// <c>&lt;SerialNumbers/&gt;</c> block) and an optional target <c>&lt;Field&gt;</c>.
/// (Earlier revisions dropped <c>&lt;Restore&gt;</c>; the skill round-trips it.)
/// </summary>
public sealed class ReplaceFieldContentsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 91;
    public const string XmlName = "Replace Field Contents";

    public bool WithDialog { get; set; }
    public bool RestoreState { get; set; }
    public FieldRef? Field { get; set; }
    public string Mode { get; set; }
    public Calculation? Calculation { get; set; }
    public SerialNumberOptions? SerialOptions { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    /// <summary>
    /// Display edits are anchor-preserved when a serial-number options block
    /// or a Restore flag is stored — the display line shows at most the
    /// skip-auto-enter marker, never the block's increment/initial-value
    /// settings, and never Restore.
    /// </summary>
    public override bool IsFullyEditable => SerialOptions is null && !RestoreState;

    private ReplaceFieldContentsStep() : this(enabled: true) { }

    public ReplaceFieldContentsStep(
        bool withDialog = true,
        FieldRef? field = null,
        string mode = "Calculation",
        Calculation? calculation = null,
        SerialNumberOptions? serialOptions = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Field = field;
        Mode = mode;
        Calculation = calculation;
        SerialOptions = serialOptions;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: the mode token is per-mode conditional (Calculation mode
    // shows the calc text, not the enum value) and the skip-auto-enter marker
    // reads SerialOptions content — grammar the shape renderer cannot express.
    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"With dialog: {(WithDialog ? "On" : "Off")}",
        };
        if (Field is not null) parts.Add(Field.ToDisplayString());
        var modePart = Mode switch
        {
            "CurrentContents" => "Current contents",
            "SerialNumbers" => "Serial numbers",
            "Calculation" => Calculation?.Text ?? "",
            _ => Mode,
        };
        parts.Add(modePart);
        if (SerialOptions is not null && !SerialOptions.PerformAutoEnter) parts.Add("Skip auto-enter options");
        return $"Replace Field Contents [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ReplaceFieldContentsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool withDialog = true;
        FieldRef? field = null;
        string mode = "Calculation";
        Calculation? calc = null;
        bool fieldSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (t.Equals("Current contents", StringComparison.OrdinalIgnoreCase))
                mode = "CurrentContents";
            else if (t.Equals("Serial numbers", StringComparison.OrdinalIgnoreCase))
                mode = "SerialNumbers";
            else if (t.Equals("None", StringComparison.OrdinalIgnoreCase))
                mode = "None";
            else if (t.Equals("Skip auto-enter options", StringComparison.OrdinalIgnoreCase))
            {
                // Marker for a stored SerialNumbers block; such steps are
                // sealed (see IsFullyEditable), so nothing to reconstruct.
            }
            else if (!fieldSeen && !string.IsNullOrWhiteSpace(t))
            {
                field = FieldRef.FromDisplayToken(t);
                fieldSeen = true;
            }
            else if (!string.IsNullOrWhiteSpace(t))
            {
                mode = "Calculation";
                calc = new Calculation(t);
            }
        }
        return new ReplaceFieldContentsStep(withDialog, field, mode, calc, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/replace-field-contents.html",
        // Canonical: NoInteract, Restore, With, then the optional replacement
        // payload (Calculation or SerialNumbers) and the optional target Field.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog" },
            new BoolStateChild("Restore") { PocoProperty = "RestoreState", Display = DisplayMode.Hidden },
            new HrOnly("Field"),
            new EnumValueChild("With") { PocoProperty = "Mode", ValidValues = ["CurrentContents", "SerialNumbers", "Calculation", "None"], DisplayValues = ["Current contents", "Serial numbers", "Calculation"], DefaultValue = "Calculation" },
            new BareCalcChild { PocoProperty = "Calculation", Optional = true },
            new ValueTypeChild("SerialNumbers") { PocoProperty = "SerialOptions", Optional = true, Display = DisplayMode.Hidden },
            new HrOnly("SerialNumbers"),
            new FieldChild("Field") { Optional = true, HrLabel = "Field" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
