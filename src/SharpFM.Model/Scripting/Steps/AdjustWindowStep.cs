using System;
using System.Collections.Generic;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for AdjustWindowStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;WindowState value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class AdjustWindowStep : ScriptStep<AdjustWindowStep>, IStepFactory
{
    public const int XmlId = 31;
    public const string XmlName = "Adjust Window";

    /// <summary>The enum XML value emitted on the <c>&lt;WindowState&gt;</c> element.</summary>
    public string WindowState { get; set; } = "ResizeToFit";

    private AdjustWindowStep() : base(false) { }

    public AdjustWindowStep(string windowState = "ResizeToFit", bool enabled = true)
        : base(enabled)
    {
        WindowState = windowState;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/adjust-window.html",
        // Single always-emitted <WindowState value="..."/> enum child.
        Shape =
        [
            new EnumValueChild("WindowState") { PocoProperty = "WindowState", DefaultValue = "ResizeToFit", ValidValues = ["Resize to Fit", "Maximize", "Minimize", "Restore", "Hide"] },
        ],
    };
}
