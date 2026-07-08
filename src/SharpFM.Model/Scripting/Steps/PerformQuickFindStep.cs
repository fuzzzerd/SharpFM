using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PerformQuickFindStep : ScriptStep, IStepFactory
{
    public const int XmlId = 150;
    public const string XmlName = "Perform Quick Find";

    public Calculation Calculation { get; set; } = new("");

    private PerformQuickFindStep() : base(false) { }

    public PerformQuickFindStep(
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
    {
        Calculation = calculation ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PerformQuickFindStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<PerformQuickFindStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-quick-find.html",
        // The bare search Calculation is omitted by the unconfigured form (Optional).
        Shape =
        [
            new BareCalcChild { PocoProperty = "Calculation", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
