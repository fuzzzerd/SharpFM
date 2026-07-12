using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Speak (66, macOS). Canonical form (skill): an optional text
/// <c>&lt;Calculation&gt;</c> followed by the <c>&lt;SpeechOptions&gt;</c>
/// element; the text calc is omitted when the step is unconfigured.
/// </summary>
public sealed class SpeakStep : ScriptStep<SpeakStep>, IStepFactory
{
    public const int XmlId = 66;
    public const string XmlName = "Speak";

    public Calculation? Text { get; set; }
    public SpeechOptions? Options { get; set; }

    /// <summary>
    /// The options FM writes for a Speak step with no voice configured:
    /// wait-for-completion on, the default (id 0) voice. This is what the
    /// display parser reconstructs, since the display line carries only the
    /// spoken text.
    /// </summary>
    internal static readonly SpeechOptions DefaultOptions = new(true, "", "0", "");

    /// <summary>
    /// Display edits are anchor-preserved when a non-default voice or
    /// wait-for-completion setting is stored — the display line cannot carry
    /// the speech options.
    /// </summary>
    public override bool IsFullyEditable => Options is null || Options == DefaultOptions;

    private SpeakStep() : base(false) { }

    public SpeakStep(Calculation? text = null, SpeechOptions? options = null, bool enabled = true)
        : base(enabled)
    {
        Text = text;
        Options = options;
    }

    // Hand-written: reconstructs the DefaultOptions SpeechOptions block the
    // wire form always carries, which the shape parser cannot synthesize.
    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        Calculation? text = null;
        if (hrParams.Length >= 1 && hrParams[0].Trim().Length > 0) text = new Calculation(hrParams[0].Trim());
        Text = text;
        Options = DefaultOptions;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/speak-os-x.html",
        // Canonical: optional text <Calculation>, then <SpeechOptions>.
        Shape =
        [
            new BareCalcChild { PocoProperty = "Text", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new ValueTypeChild("SpeechOptions") { PocoProperty = "Options", Display = DisplayMode.Hidden },
            new HrOnly("SpeechOptions") { HrLabel = "Speech options" },
        ],
        Notes = new StepNotes
        {
            Platform = new StepPlatformNotes
            {
                Server = "Not supported.",
                WebDirect = "Not supported.",
                Go = "Not supported.",
            },
        },
    };
}
