using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for EnableTouchKeyboardStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;ShowHide value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class EnableTouchKeyboardStep : ScriptStep, IStepFactory
{
    public const int XmlId = 174;
    public const string XmlName = "Enable Touch Keyboard";

    /// <summary>The enum XML value emitted on the <c>&lt;ShowHide&gt;</c> element.</summary>
    public string ShowHide { get; set; } = "Show";

    private EnableTouchKeyboardStep() : base(false) { }

    public EnableTouchKeyboardStep(string showHide = "Show", bool enabled = true)
        : base(enabled)
    {
        ShowHide = showHide;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<EnableTouchKeyboardStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<EnableTouchKeyboardStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/enable-touch-keyboard.html",
        // Single always-emitted <ShowHide value="..."/> enum child.
        Shape =
        [
            new EnumValueChild("ShowHide") { PocoProperty = "ShowHide", DefaultValue = "Show", ValidValues = ["On", "Off", "Toggle"] },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
