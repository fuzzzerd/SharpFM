using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class OpenURLStep : ScriptStep, IStepFactory
{
    public const int XmlId = 111;
    public const string XmlName = "Open URL";

    public bool WithDialog { get; set; }
    public bool InExternalBrowser { get; set; }
    public Calculation Calculation { get; set; } = new("");

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private OpenURLStep() : base(false) { }

    public OpenURLStep(
        bool withDialog = true,
        bool inExternalBrowser = false,
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        InExternalBrowser = inExternalBrowser;
        Calculation = calculation ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<OpenURLStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<OpenURLStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-url.html",
        // Canonical: NoInteract, Option, then the URL Calculation which the
        // unconfigured form omits (Optional). <NoInteract state> inverts WithDialog.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Augmented, DisplayInverted = true },
            new BoolStateChild("Option") { PocoProperty = "InExternalBrowser", HrLabel = "In external browser", Display = DisplayMode.Augmented },
            new BareCalcChild { PocoProperty = "Calculation", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
