using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PrintStep : ScriptStep, IStepFactory
{
    public const int XmlId = 43;
    public const string XmlName = "Print";

    public bool WithDialog { get; set; }
    public bool RestoreStoredSettings { get; set; }
    public PrintSettings? Settings { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private PrintStep() : base(false) { }

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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Print [ With dialog: {(WithDialog ? "On" : "Off")} ; Restore: {(RestoreStoredSettings ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PrintStep>(step, Metadata);

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
        // Canonical: NoInteract (inverts WithDialog), Restore, then the optional
        // PrintSettings which owns its own attribute/PlatformData shape.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Augmented },
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredSettings", HrLabel = "Restore", Display = DisplayMode.Augmented },
            new ValueTypeChild("PrintSettings") { PocoProperty = "Settings", Optional = true, Display = DisplayMode.Hidden },
        ],
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
