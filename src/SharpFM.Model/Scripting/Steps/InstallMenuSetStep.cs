using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InstallMenuSetStep : ScriptStep<InstallMenuSetStep>, IStepFactory
{
    public const int XmlId = 142;
    public const string XmlName = "Install Menu Set";

    public NamedRef MenuSet { get; set; } = new(0, "");
    public bool UseAsFileDefault { get; set; }

    private InstallMenuSetStep() : base(false) { }

    public InstallMenuSetStep(NamedRef? menuSet = null, bool useAsFileDefault = false, bool enabled = true)
        : base(enabled)
    {
        MenuSet = menuSet ?? new NamedRef(0, "");
        UseAsFileDefault = useAsFileDefault;
    }

    // Hand-written: quoted menu-set name token the shape renderer cannot produce.
    public override string ToDisplayLine() =>
        $"Install Menu Set [ \"{MenuSet.Name}\" ; Use as file default: {(UseAsFileDefault ? "On" : "Off")} ]";

    protected internal override void PopulateFromDisplay(string[] hrParams)
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
                // The built-in menu set has the fixed id 1; custom menu sets
                // carry file-specific ids the canonical form wildcards.
                menu = new NamedRef(name == "[Standard FileMaker Menus]" ? 1 : 0, name);
                menuSeen = true;
            }
        }
        MenuSet = menu;
        UseAsFileDefault = useDefault;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/install-menu-set.html",
        // Canonical: UseAsFileDefault, then the always-present CustomMenuSet ref.
        Shape =
        [
            new BoolStateChild("UseAsFileDefault") { PocoProperty = "UseAsFileDefault", HrLabel = "Use as file default", Display = DisplayMode.Augmented },
            new NamedRefChild("CustomMenuSet") { PocoProperty = "MenuSet", Required = true, Display = DisplayMode.Native },
        ],
    };
}
