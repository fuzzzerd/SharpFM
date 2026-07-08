using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ShowHideTextRulerStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;ShowHide value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class ShowHideTextRulerStep : ScriptStep, IStepFactory
{
    public const int XmlId = 92;
    public const string XmlName = "Show/Hide Text Ruler";

    /// <summary>The enum XML value emitted on the <c>&lt;ShowHide&gt;</c> element.</summary>
    public string Action { get; set; }

    private ShowHideTextRulerStep() : base(false)
    {
        Action = "Show";
    }

    public ShowHideTextRulerStep(string action = "Show", bool enabled = true)
        : base(enabled)
    {
        Action = action;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ShowHideTextRulerStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<ShowHideTextRulerStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/show-hide-text-ruler.html",
        Shape =
        [
            new EnumValueChild("ShowHide") { PocoProperty = "Action", HrLabel = "Action", DefaultValue = "Show", ValidValues = ["Show", "Hide", "Toggle"] },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
