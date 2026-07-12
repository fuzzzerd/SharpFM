using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class DialPhoneStep : ScriptStep<DialPhoneStep>, IStepFactory
{
    public const int XmlId = 65;
    public const string XmlName = "Dial Phone";

    public bool WithDialog { get; set; }
    public bool UseDialPreferences { get; set; }
    public Calculation? Calculation { get; set; }

    /// <summary>
    /// XML-facing inverse of <see cref="WithDialog"/>: canonical
    /// <c>&lt;NoInteract state="…"/&gt;</c> suppresses the dialog, so it is the
    /// negation of the display-facing flag. The shape binds this so the raw
    /// state round-trips without re-applying the inversion.
    /// </summary>
    public bool NoInteract { get => !WithDialog; set => WithDialog = !value; }

    /// <summary>
    /// XML-facing view of <see cref="UseDialPreferences"/>: the canonical form
    /// omits <c>&lt;UseDialPreferences&gt;</c> until it is enabled, so this is
    /// null (omitted by the Optional node) when the flag is off.
    /// </summary>
    public string? UseDialPreferencesValue
    {
        get => UseDialPreferences ? "True" : null;
        set => UseDialPreferences = value == "True";
    }

    private DialPhoneStep() : base(false) { }

    public DialPhoneStep(
        bool withDialog = true,
        bool useDialPreferences = false,
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        UseDialPreferences = useDialPreferences;
        Calculation = calculation;
    }

    // Hand-written: the bare On/Off token maps the wire's "True"/absent
    // UseDialPreferences value, a translation the display metadata cannot
    // express without widening the node's wire ValidValues.
    public override string ToDisplayLine() =>
        "Dial Phone [ " + "With dialog: " + (WithDialog ? "On" : "Off") + " ; " + (UseDialPreferences ? "On" : "Off") + " ; " + (Calculation?.Text ?? "") + " ]";

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool withDialog_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        // Display shape is positional after the dialog flag: [ With dialog ; use-dial-prefs On/Off ; phone calc ].
        bool useDialPreferences_v = tokens.Length >= 2 && tokens[1].Equals("On", StringComparison.OrdinalIgnoreCase);
        Calculation? calculation_v = tokens.Length >= 3 && tokens[2].Length > 0 ? new Calculation(tokens[2]) : null;
        WithDialog = withDialog_v;
        UseDialPreferences = useDialPreferences_v;
        Calculation = calculation_v;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/dial-phone.html",
        // Canonical 065-DialPhone: only NoInteract (always present, the inverse
        // of WithDialog). <UseDialPreferences> and the bare <Calculation> are
        // omitted until configured, so both are Optional.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteract", HrLabel = "With dialog" },
            new EnumValueChild("UseDialPreferences") { PocoProperty = "UseDialPreferencesValue", Optional = true, DisplayValues = ["On", "Off"] },
            new BareCalcChild { PocoProperty = "Calculation", Optional = true },
        ],
    };
}
