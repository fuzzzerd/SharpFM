using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class OmitMultipleRecordsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 26;
    public const string XmlName = "Omit Multiple Records";

    public bool WithDialog { get; set; }
    public Calculation? Calculation { get; set; }

    /// <summary>
    /// XML-facing inverse of <see cref="WithDialog"/>: canonical
    /// <c>&lt;NoInteract state="…"/&gt;</c> suppresses the dialog, so it is the
    /// negation of the display-facing flag. The shape binds this so the raw
    /// state round-trips without re-applying the inversion.
    /// </summary>
    public bool NoInteract { get => !WithDialog; set => WithDialog = !value; }

    private OmitMultipleRecordsStep() : base(false) { }

    public OmitMultipleRecordsStep(
        bool withDialog = true,
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Calculation = calculation;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<OmitMultipleRecordsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<OmitMultipleRecordsStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/omit-multiple-records.html",
        // Canonical 026-OmitMultipleRecords: NoInteract (always present, the
        // inverse of WithDialog) then a bare <Calculation> that is omitted when
        // blank (Optional).
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteract", HrLabel = "With dialog", DisplayInverted = true },
            new BareCalcChild { PocoProperty = "Calculation", Optional = true, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
