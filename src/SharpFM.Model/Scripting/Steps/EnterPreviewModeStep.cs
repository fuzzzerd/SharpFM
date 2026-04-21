using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for EnterPreviewModeStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Pause state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class EnterPreviewModeStep : ScriptStep, IStepFactory
{
    public const int XmlId = 41;
    public const string XmlName = "Enter Preview Mode";

    /// <summary>The <c>Pause</c> flag on the step.</summary>
    public bool Pause { get; set; }

    public EnterPreviewModeStep(bool pause = false, bool enabled = true)
        : base(null, enabled)
    {
        Pause = pause;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Pause",
                new XAttribute("state", Pause ? "True" : "False")));

    public override string ToDisplayLine() =>
        $"Enter Preview Mode [ Pause: {(Pause ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var state = step.Element("Pause")?.Attribute("state")?.Value == "True";
        return new EnterPreviewModeStep(state, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        const string Prefix = "Pause:";
        if (token.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            token = token.Substring(Prefix.Length).Trim();
        var isOn = token.Equals("On", StringComparison.OrdinalIgnoreCase);
        return new EnterPreviewModeStep(isOn, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/enter-preview-mode.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Pause",
                XmlElement = "Pause",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Pause",
                ValidValues = ["On", "Off"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
