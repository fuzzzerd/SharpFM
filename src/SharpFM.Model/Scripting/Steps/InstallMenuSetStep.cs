using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InstallMenuSetStep : ScriptStep, IStepFactory
{
    public const int XmlId = 142;
    public const string XmlName = "Install Menu Set";

    public NamedRef MenuSet { get; set; }
    public bool UseAsFileDefault { get; set; }

    public InstallMenuSetStep(NamedRef? menuSet = null, bool useAsFileDefault = false, bool enabled = true)
        : base(enabled)
    {
        MenuSet = menuSet ?? new NamedRef(0, "");
        UseAsFileDefault = useAsFileDefault;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("UseAsFileDefault", new XAttribute("state", UseAsFileDefault ? "True" : "False")),
            MenuSet.ToXml("CustomMenuSet"));

    public override string ToDisplayLine() =>
        $"Install Menu Set [ \"{MenuSet.Name}\" ; Use as file default: {(UseAsFileDefault ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var useDefault = step.Element("UseAsFileDefault")?.Attribute("state")?.Value == "True";
        var menuEl = step.Element("CustomMenuSet");
        var menu = menuEl is not null ? NamedRef.FromXml(menuEl) : new NamedRef(0, "");
        return new InstallMenuSetStep(menu, useDefault, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        NamedRef menu = new(0, "");
        bool useDefault = false;
        bool menuSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Use as file default:", StringComparison.OrdinalIgnoreCase))
            {
                useDefault = t.Substring(20).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            }
            else if (!menuSeen && !string.IsNullOrWhiteSpace(t))
            {
                var name = t;
                if (name.StartsWith("\"") && name.EndsWith("\"") && name.Length >= 2)
                    name = name.Substring(1, name.Length - 2);
                menu = new NamedRef(0, name);
                menuSeen = true;
            }
        }
        return new InstallMenuSetStep(menu, useDefault, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/install-menu-set.html",
        Params =
        [
            new ParamMetadata { Name = "CustomMenuSet", XmlElement = "CustomMenuSet", Type = "menuSet", Required = true },
            new ParamMetadata
            {
                Name = "UseAsFileDefault",
                XmlElement = "UseAsFileDefault",
                XmlAttr = "state",
                Type = "boolean",
                HrLabel = "Use as file default",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
