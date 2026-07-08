using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ViewAsStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;View value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class ViewAsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 30;
    public const string XmlName = "View As";

    /// <summary>The enum XML value emitted on the <c>&lt;View&gt;</c> element.</summary>
    public string View { get; set; }

    private ViewAsStep() : base(false)
    {
        View = "Cycle";
    }

    public ViewAsStep(string view = "Cycle", bool enabled = true)
        : base(enabled)
    {
        View = view;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ViewAsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<ViewAsStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/view-as.html",
        Shape =
        [
            new EnumValueChild("View") { HrLabel = "View", DefaultValue = "Cycle", ValidValues = ["Cycle", "Form", "List", "Table"], DisplayValues = ["Cycle", "View as Form", "View as List", "View as Table"] },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
