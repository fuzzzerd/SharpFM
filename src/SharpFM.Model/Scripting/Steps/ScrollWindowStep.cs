using System;
using System.Collections.Generic;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ScrollWindowStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;ScrollOperation value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class ScrollWindowStep : ScriptStep<ScrollWindowStep>, IStepFactory
{
    public const int XmlId = 81;
    public const string XmlName = "Scroll Window";

    /// <summary>The enum XML value emitted on the <c>&lt;ScrollOperation&gt;</c> element.</summary>
    public string Direction { get; set; } = "Home";

    private ScrollWindowStep() : base(false) { }

    public ScrollWindowStep(string direction = "Home", bool enabled = true)
        : base(enabled)
    {
        Direction = direction;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/scroll-window.html",
        // Canonical: a single ScrollOperation enum child.
        Shape =
        [
            new EnumValueChild("ScrollOperation")
            {
                PocoProperty = "Direction",
                HrLabel = "Direction",
                DefaultValue = "Home",
                ValidValues = ["Home", "End", "PageUp", "PageDown", "ToSelection"],
                DisplayValues = ["Home", "End", "Page Up", "Page Down", "To Selection"],
                Display = DisplayMode.Native,
            },
        ],
    };
}
