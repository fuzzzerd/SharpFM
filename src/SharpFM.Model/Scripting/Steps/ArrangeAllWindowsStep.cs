using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ArrangeAllWindowsStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;WindowArrangement value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class ArrangeAllWindowsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 120;
    public const string XmlName = "Arrange All Windows";

    /// <summary>The enum XML value emitted on the <c>&lt;WindowArrangement&gt;</c> element.</summary>
    public string WindowArrangement { get; set; } = "Cascade Window";

    private ArrangeAllWindowsStep() : base(false) { }

    public ArrangeAllWindowsStep(string windowArrangement = "Cascade Window", bool enabled = true)
        : base(enabled)
    {
        WindowArrangement = windowArrangement;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ArrangeAllWindowsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<ArrangeAllWindowsStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/arrange-all-windows.html",
        // Single always-emitted <WindowArrangement value="..."/> enum child.
        Shape =
        [
            new EnumValueChild("WindowArrangement") { PocoProperty = "WindowArrangement", DefaultValue = "Cascade Window", ValidValues = ["Tile Horizontally", "Tile Vertically", "Cascade Window", "Bring All To Front"] },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
