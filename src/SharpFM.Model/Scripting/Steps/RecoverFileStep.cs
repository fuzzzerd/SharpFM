using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RecoverFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 95;
    public const string XmlName = "Recover File";

    public bool WithDialog { get; set; }
    public string UniversalPathList { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private RecoverFileStep() : base(false) { }

    public RecoverFileStep(
        bool withDialog = true,
        string universalPathList = "",
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        UniversalPathList = universalPathList;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<RecoverFileStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<RecoverFileStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/recover-file.html",
        // Canonical: NoInteract then UniversalPathList; the path list is omitted
        // when empty (Optional), and <NoInteract state> is the inverse of WithDialog.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Augmented, DisplayInverted = true },
            new NamedTextChild("UniversalPathList") { PocoProperty = "UniversalPathList", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
