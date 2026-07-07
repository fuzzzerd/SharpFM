using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for DeleteRecordRequestStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;NoInteract state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class DeleteRecordRequestStep : ScriptStep, IStepFactory
{
    public const int XmlId = 9;
    public const string XmlName = "Delete Record/Request";

    /// <summary>The <c>With dialog</c> flag on the step.</summary>
    public bool WithDialog { get; set; }

    /// <summary>
    /// XML-facing inverse of <see cref="WithDialog"/>: canonical
    /// <c>&lt;NoInteract state="…"/&gt;</c> suppresses the dialog, so it is the
    /// negation of the display-facing flag. The shape binds this so the raw
    /// state round-trips without re-applying the inversion.
    /// </summary>
    public bool NoInteract { get => !WithDialog; set => WithDialog = !value; }

    private DeleteRecordRequestStep() : base(false) { }

    public DeleteRecordRequestStep(bool withdialog = false, bool enabled = true)
        : base(enabled)
    {
        WithDialog = withdialog;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<DeleteRecordRequestStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<DeleteRecordRequestStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/delete-record-request.html",
        // Single always-emitted <NoInteract state="..."/> child (inverse of WithDialog).
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteract", HrLabel = "With dialog", DisplayInverted = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
