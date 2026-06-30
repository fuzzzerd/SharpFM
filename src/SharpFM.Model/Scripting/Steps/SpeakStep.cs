using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Speak (66, macOS). Canonical form (skill): an optional text
/// <c>&lt;Calculation&gt;</c> followed by the <c>&lt;SpeechOptions&gt;</c>
/// element; the text calc is omitted when the step is unconfigured.
/// </summary>
public sealed class SpeakStep : ScriptStep, IStepFactory
{
    public const int XmlId = 66;
    public const string XmlName = "Speak";

    public Calculation? Text { get; set; }
    public SpeechOptions? Options { get; set; }

    private SpeakStep() : base(false) { }

    public SpeakStep(Calculation? text = null, SpeechOptions? options = null, bool enabled = true)
        : base(enabled)
    {
        Text = text;
        Options = options;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => $"Speak [ {Text?.Text ?? ""} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SpeakStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        Calculation text = new("");
        if (hrParams.Length >= 1) text = new Calculation(hrParams[0].Trim());
        return new SpeakStep(text, null, enabled);
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
            new BareCalcChild { PocoProperty = "Text", HrLabel = "Text to speak", Optional = true, Display = DisplayMode.Native },
            new ValueTypeChild("SpeechOptions") { PocoProperty = "Options", Display = DisplayMode.Hidden },
        ],
        Params =
        [
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", HrLabel = "Text to speak", Required = true },
            new ParamMetadata { Name = "SpeechOptions", XmlElement = "SpeechOptions", Type = "complex", HrLabel = "Speech options" },
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
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
