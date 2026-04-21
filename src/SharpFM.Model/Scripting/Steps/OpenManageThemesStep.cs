using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for OpenManageThemesStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class OpenManageThemesStep : ScriptStep, IStepFactory
{
    public const int XmlId = 165;
    public const string XmlName = "Open Manage Themes";

    public OpenManageThemesStep(bool enabled = true) : base(enabled) { }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

    public override string ToDisplayLine() => XmlName;

    public static new ScriptStep FromXml(XElement step) =>
        new OpenManageThemesStep(step.Attribute("enable")?.Value != "False");

    public static ScriptStep FromDisplayParams(bool enabled, string[] _) =>
        new OpenManageThemesStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "open menu item",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-manage-themes.html",
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
