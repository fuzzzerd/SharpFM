using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for RevertRecordRequestStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;NoInteract state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class RevertRecordRequestStep : ScriptStep<RevertRecordRequestStep>, IStepFactory
{
    public const int XmlId = 51;
    public const string XmlName = "Revert Record/Request";

    /// <summary>The <c>With dialog</c> flag on the step.</summary>
    public bool WithDialog { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private RevertRecordRequestStep() : base(false) { }

    public RevertRecordRequestStep(bool withdialog = true, bool enabled = true)
        : base(enabled)
    {
        WithDialog = withdialog;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/revert-record-request.html",
        // Canonical: a single NoInteract child (inverts WithDialog).
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Augmented, DisplayInverted = true },
        ],
    };
}
