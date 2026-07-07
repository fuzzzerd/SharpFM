using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class DialPhoneStep : ScriptStep, IStepFactory
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Dial Phone [ " + "With dialog: " + (WithDialog ? "On" : "Off") + " ; " + (UseDialPreferences ? "On" : "Off") + " ; " + (Calculation?.Text ?? "") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<DialPhoneStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool withDialog_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool useDialPreferences_v = false;
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))) { calculation_v = new Calculation(tok); break; } }
        return new DialPhoneStep(withDialog_v, useDialPreferences_v, calculation_v, enabled);
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
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
