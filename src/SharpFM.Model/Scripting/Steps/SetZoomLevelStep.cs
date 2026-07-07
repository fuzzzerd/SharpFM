using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetZoomLevelStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus one child element per metadata param.
/// Display form uses labeled segments joined by ' ; '; FromDisplayParams
/// scans tokens by label so segment order is free.
/// </summary>
public sealed class SetZoomLevelStep : ScriptStep, IStepFactory
{
    public const int XmlId = 97;
    public const string XmlName = "Set Zoom Level";

    public bool Lock { get; set; }
    public string ZoomLevel { get; set; }
    public Calculation? ZoomCalculation { get; set; }

    /// <summary>
    /// Display edits are anchor-preserved when a zoom-by-calculation
    /// expression is stored — the display line shows only the ByCalculation
    /// marker, never the calculation itself.
    /// </summary>
    public override bool IsFullyEditable => ZoomCalculation is null;

    private SetZoomLevelStep() : base(false) { ZoomLevel = "100"; }

    public SetZoomLevelStep(
        bool @lock = false,
        string zoomLevel = "100",
        Calculation? zoomCalculation = null,
        bool enabled = true)
        : base(enabled)
    {
        Lock = @lock;
        ZoomLevel = zoomLevel;
        ZoomCalculation = zoomCalculation;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetZoomLevelStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<SetZoomLevelStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-zoom-level.html",
        // Canonical: Lock, Zoom, and (only for the ByCalculation zoom) a bare
        // <Calculation> carrying the zoom-percentage expression.
        Shape =
        [
            new BoolStateChild("Lock") { PocoProperty = "Lock", HrLabel = "Lock", Display = DisplayMode.Native },
            new EnumValueChild("Zoom") { PocoProperty = "ZoomLevel", HrLabel = "Zoom level", DefaultValue = "100", ValidValues = ["25", "50", "75", "100", "150", "200", "300", "400", "ZoomIn", "ZoomOut", "ByCalculation"], DisplayValues = ["25%", "50%", "75%", "100%", "150%", "200%", "300%", "400%", "Zoom In", "Zoom Out", "ByCalculation"], Display = DisplayMode.Native },
            new BareCalcChild { PocoProperty = "ZoomCalculation", Optional = true, Display = DisplayMode.Hidden },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
