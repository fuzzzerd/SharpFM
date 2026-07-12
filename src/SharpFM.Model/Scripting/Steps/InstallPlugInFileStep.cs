using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InstallPlugInFileStep : ScriptStep<InstallPlugInFileStep>, IStepFactory
{
    public const int XmlId = 157;
    public const string XmlName = "Install Plug-In File";

    // Nullable so the unconfigured form (no Field) omits the optional <Field> node.
    public FieldRef? Target { get; set; }

    private InstallPlugInFileStep() : base(false) { }

    public InstallPlugInFileStep(
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        Target = target;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/install-plug-in-file.html",
        // The target Field is omitted by the unconfigured form (Optional).
        Shape =
        [
            new FieldChild("Field") { PocoProperty = "Target", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
    };
}
