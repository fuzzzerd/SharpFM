using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExitScriptStep : ScriptStep<ExitScriptStep>, IStepFactory
{
    public const int XmlId = 103;
    public const string XmlName = "Exit Script";

    public Calculation Calculation { get; set; } = new("");

    private ExitScriptStep() : base(false) { }

    public ExitScriptStep(
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
    {
        Calculation = calculation ?? new Calculation("");
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/exit-script.html",
        // The bare return Calculation is omitted by the unconfigured form (Optional).
        Shape =
        [
            new BareCalcChild { PocoProperty = "Calculation", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
    };
}
