using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for RefreshWindowStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus one child element per metadata param.
/// Display form uses labeled segments joined by ' ; '; FromDisplayParams
/// scans tokens by label so segment order is free.
/// </summary>
public sealed class RefreshWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 80;
    public const string XmlName = "Refresh Window";

    public bool FlushCachedJoinResults { get; set; }
    public bool FlushCachedExternalData { get; set; }

    private RefreshWindowStep() : base(false) { }

    public RefreshWindowStep(
        bool flushCachedJoinResults = false,
        bool flushCachedExternalData = false,
        bool enabled = true)
        : base(enabled)
    {
        FlushCachedJoinResults = flushCachedJoinResults;
        FlushCachedExternalData = flushCachedExternalData;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<RefreshWindowStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<RefreshWindowStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/refresh-window.html",
        // Canonical: Option (flush cached join results), then FlushSQLData.
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "FlushCachedJoinResults", HrLabel = "Flush cached join results", Display = DisplayMode.Augmented },
            new BoolStateChild("FlushSQLData") { PocoProperty = "FlushCachedExternalData", HrLabel = "Flush cached external data", Display = DisplayMode.Augmented },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
