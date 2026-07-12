using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PrintSetupStep : ScriptStep<PrintSetupStep>, IStepFactory
{
    public const int XmlId = 42;
    public const string XmlName = "Print Setup";

    public bool WithDialog { get; set; }
    public bool RestoreStoredSettings { get; set; }
    public PageFormat? Format { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private PrintSetupStep() : base(false) { }

    public PrintSetupStep(
        bool withDialog = false,
        bool restoreStoredSettings = true,
        PageFormat? format = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        RestoreStoredSettings = restoreStoredSettings;
        Format = format;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/print-setup.html",
        // Canonical: NoInteract (inverts WithDialog), Restore, then the optional
        // PageFormat which owns its own attribute/PlatformData shape.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Augmented, DisplayInverted = true },
            new BoolStateChild("Restore") { PocoProperty = "RestoreStoredSettings", HrLabel = "Restore", Display = DisplayMode.Augmented },
            new ValueTypeChild("PageFormat") { PocoProperty = "Format", Optional = true, Display = DisplayMode.Hidden },
            new HrOnly("Restore") { Boolean = true },
            new HrOnly("PageFormat"),
        ],
    };
}
