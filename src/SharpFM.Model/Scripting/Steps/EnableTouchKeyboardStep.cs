using System;
using System.Collections.Generic;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for EnableTouchKeyboardStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;ShowHide value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class EnableTouchKeyboardStep : ScriptStep<EnableTouchKeyboardStep>, IStepFactory
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
    };
}
