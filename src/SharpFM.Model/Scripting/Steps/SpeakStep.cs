using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SpeakStep : ScriptStep, IStepFactory
{
    public const int XmlId = 66;
    public const string XmlName = "Speak";

    public Calculation Text { get; set; }
    public SpeechOptions? Options { get; set; }

    public SpeakStep(Calculation? text = null, SpeechOptions? options = null, bool enabled = true)
        : base(enabled)
    {
        Text = text ?? new Calculation("");
        Options = options;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            Text.ToXml("Calculation"));
        if (Options is not null) step.Add(Options.ToXml());
        return step;
    }

    public override string ToDisplayLine() => $"Speak [ {Text.Text} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var calcEl = step.Element("Calculation");
        var text = calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");
        var optEl = step.Element("SpeechOptions");
        var options = optEl is not null ? SpeechOptions.FromXml(optEl) : null;
        return new SpeakStep(text, options, enabled);
    }

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
