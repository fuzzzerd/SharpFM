using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetLayoutObjectAnimationStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Set state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class SetLayoutObjectAnimationStep : ScriptStep<SetLayoutObjectAnimationStep>, IStepFactory
{
    public const int XmlId = 168;
    public const string XmlName = "Set Layout Object Animation";

    /// <summary>The <c>Animation</c> flag on the step.</summary>
    public bool Animation { get; set; }

    private SetLayoutObjectAnimationStep() : base(false) { }

    public SetLayoutObjectAnimationStep(bool animation = true, bool enabled = true)
        : base(enabled)
    {
        Animation = animation;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-layout-object-animation.html",
        Shape =
        [
            new BoolStateChild("Set") { PocoProperty = "Animation", HrLabel = "Animation" },
        ],
    };
}
