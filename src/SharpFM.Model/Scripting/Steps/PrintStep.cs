using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PrintStep : ScriptStep, IStepFactory
{
    public const int XmlId = 43;
    public const string XmlName = "Print";

    public bool WithDialog { get; set; }
    public bool RestoreStoredSettings { get; set; }
    public PrintSettings? Settings { get; set; }

    public PrintStep(
        bool withDialog = false,
        bool restoreStoredSettings = true,
        PrintSettings? settings = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        RestoreStoredSettings = restoreStoredSettings;
        Settings = settings;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("Restore", new XAttribute("state", RestoreStoredSettings ? "True" : "False")));
        if (Settings is not null) step.Add(Settings.ToXml());
        return step;
    }

    public override string ToDisplayLine() =>
        $"Print [ With dialog: {(WithDialog ? "On" : "Off")} ; Restore: {(RestoreStoredSettings ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var restore = step.Element("Restore")?.Attribute("state")?.Value == "True";
        var settingsEl = step.Element("PrintSettings");
        var settings = settingsEl is not null ? PrintSettings.FromXml(settingsEl) : null;
        return new PrintStep(withDialog, restore, settings, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool withDialog = false, restore = true;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", System.StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Restore:", System.StringComparison.OrdinalIgnoreCase))
                restore = t.Substring(8).Trim().Equals("On", System.StringComparison.OrdinalIgnoreCase);
        }
        return new PrintStep(withDialog, restore, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/print.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "Restore", XmlElement = "Restore", XmlAttr = "state", Type = "boolean" },
            new ParamMetadata { Name = "PrintSettings", XmlElement = "PrintSettings", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
