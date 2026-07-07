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

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PrintStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<PrintStep>(enabled, hrParams, Metadata);

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
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Augmented, DisplayInverted = true },
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredSettings", HrLabel = "Restore", Display = DisplayMode.Augmented },
            new ValueTypeChild("PrintSettings") { PocoProperty = "Settings", Optional = true, Display = DisplayMode.Hidden },
            new HrOnly("Restore") { Boolean = true },
            new HrOnly("PrintSettings"),
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
