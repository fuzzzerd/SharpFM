using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetRevertTransactionOnErrorStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Set state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class SetRevertTransactionOnErrorStep : ScriptStep<SetRevertTransactionOnErrorStep>, IStepFactory
{
    public const int XmlId = 223;
    public const string XmlName = "Set Revert Transaction on Error";

    /// <summary>The <c>Revert on error</c> flag on the step.</summary>
    public bool RevertOnError { get; set; }

    private SetRevertTransactionOnErrorStep() : base(false) { }

    public SetRevertTransactionOnErrorStep(bool revertonerror = false, bool enabled = true)
        : base(enabled)
    {
        RevertOnError = revertonerror;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-revert-transaction-on-error.html",
        Shape =
        [
            new BoolStateChild("Set") { PocoProperty = "RevertOnError", HrLabel = "Revert on error" },
        ],
    };
}
