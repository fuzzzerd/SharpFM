using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Cancel PDF aborts an in-progress multi-step PDF assembly. It carries no
/// parameters — the canonical form is an empty <c>&lt;Step&gt;</c> element.
/// </summary>
public sealed class CancelPdfStep : ScriptStep<CancelPdfStep>, IStepFactory
{
    public const int XmlId = 247;
    public const string XmlName = "Cancel PDF";

    private CancelPdfStep() : base(false) { }

    public CancelPdfStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/cancel-pdf.html",
    };
}
