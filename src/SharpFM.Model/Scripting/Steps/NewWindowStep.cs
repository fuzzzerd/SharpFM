using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// New Window (122). Canonical form (skill, windows reference): <c>LayoutDestination</c>,
/// then the optional window <c>Name</c> and the optional dimension calculations
/// (<c>Height</c>, <c>Width</c>, <c>DistanceFromTop</c>, <c>DistanceFromLeft</c>),
/// then the always-present <c>NewWndStyles</c> attribute element (driven by the
/// <see cref="NewWindowStyles"/> value type), then the optional target
/// <c>Layout</c>. Unconfigured windows omit every optional child.
/// </summary>
public sealed class NewWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 122;
    public const string XmlName = "New Window";

    public string LayoutDestination { get; set; } = "SelectedLayout";
    public Calculation? Name { get; set; }
    public Calculation? Height { get; set; }
    public Calculation? Width { get; set; }
    public Calculation? DistanceFromTop { get; set; }
    public Calculation? DistanceFromLeft { get; set; }
    public NewWindowStyles Styles { get; set; } = NewWindowStyles.Default();
    public NamedRef? Layout { get; set; }

    private NewWindowStep() : base(false) { }

    public NewWindowStep(bool enabled = true) : base(enabled) { }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<NewWindowStep>(step, Metadata);

    /// <summary>
    /// Display edits are anchor-preserved when state the display line cannot
    /// carry is present: a target layout, window dimensions, a non-default
    /// styles block, or a layout destination other than the unconfigured
    /// <c>CurrentLayout</c> form the display parser reconstructs.
    /// </summary>
    public override bool IsFullyEditable =>
        LayoutDestination == "CurrentLayout"
        && Layout is null
        && Height is null && Width is null
        && DistanceFromTop is null && DistanceFromLeft is null
        && Styles == NewWindowStyles.Default();

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Hand-written: reconstructs the unconfigured LayoutDestination
        // "CurrentLayout" wire form, which the shape parser cannot synthesize
        // for a display-hidden slot; anything beyond a window name is sealed state.
        var step = new NewWindowStep(enabled) { LayoutDestination = "CurrentLayout" };
        var name = hrParams.Select(h => h.Trim()).FirstOrDefault(t => t.Length > 0);
        if (!string.IsNullOrEmpty(name)) step.Name = new Calculation(name);
        return step;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/new-window.html",
        Shape =
        [
            new EnumValueChild("LayoutDestination") { PocoProperty = "LayoutDestination", DefaultValue = "SelectedLayout", Display = DisplayMode.Hidden },
            // Name renders as a bare token; the geometry is sealed state the
            // display line never carries (see IsFullyEditable).
            new NamedCalcChild("Name") { PocoProperty = "Name", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("Height") { PocoProperty = "Height", HrLabel = "Height", Optional = true, Display = DisplayMode.Hidden },
            new NamedCalcChild("Width") { PocoProperty = "Width", HrLabel = "Width", Optional = true, Display = DisplayMode.Hidden },
            new NamedCalcChild("DistanceFromTop") { PocoProperty = "DistanceFromTop", HrLabel = "Top", Optional = true, Display = DisplayMode.Hidden },
            new NamedCalcChild("DistanceFromLeft") { PocoProperty = "DistanceFromLeft", HrLabel = "Left", Optional = true, Display = DisplayMode.Hidden },
            new ValueTypeChild("NewWndStyles") { PocoProperty = "Styles", Display = DisplayMode.Hidden },
            new NamedRefChild("Layout") { PocoProperty = "Layout", Optional = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
