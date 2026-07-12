using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for EnterBrowseModeStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Pause state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class EnterBrowseModeStep : ScriptStep<EnterBrowseModeStep>, IStepFactory
{
    public const int XmlId = 55;
    public const string XmlName = "Enter Browse Mode";

    /// <summary>The <c>Pause</c> flag on the step.</summary>
    public bool Pause { get; set; }

    private EnterBrowseModeStep() : base(false) { }

    public EnterBrowseModeStep(bool pause = false, bool enabled = true)
        : base(enabled)
    {
        Pause = pause;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/enter-browse-mode.html",
        // Single always-emitted <Pause state="..."/> child.
        Shape =
        [
            new BoolStateChild("Pause") { PocoProperty = "Pause", HrLabel = "Pause" },
        ],
    };
}
