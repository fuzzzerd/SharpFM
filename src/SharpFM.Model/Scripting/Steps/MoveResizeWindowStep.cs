using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class MoveResizeWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 119;
    public const string XmlName = "Move/Resize Window";

    public string Window { get; set; } = "ByName";
    public Calculation Name { get; set; } = new("");
    public bool CurrentFile { get; set; }
    public Calculation Height { get; set; } = new("");
    public Calculation Width { get; set; } = new("");
    public Calculation Top { get; set; } = new("");
    public Calculation Left { get; set; } = new("");

    private MoveResizeWindowStep() : base(false) { }

    public MoveResizeWindowStep(
        string window = "ByName",
        Calculation? name = null,
        bool currentFile = true,
        Calculation? height = null,
        Calculation? width = null,
        Calculation? top = null,
        Calculation? left = null,
        bool enabled = true)
        : base(enabled)
    {
        Window = window;
        Name = name ?? new Calculation("");
        CurrentFile = currentFile;
        Height = height ?? new Calculation("");
        Width = width ?? new Calculation("");
        Top = top ?? new Calculation("");
        Left = left ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<MoveResizeWindowStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<MoveResizeWindowStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/move-resize-window.html",
        // Canonical: LimitToWindowsOfCurrentFile leads, then Window, then the
        // optional Name and geometry calculations (omitted when unconfigured).
        Shape =
        [
            // Native slots (Window, Name) lead the display line; Augmented
            // (Current file, geometry) follow in shape order — XML order unchanged.
            new BoolStateChild("LimitToWindowsOfCurrentFile") { PocoProperty = "CurrentFile", HrLabel = "Current file", Display = DisplayMode.Augmented },
            new EnumValueChild("Window") { PocoProperty = "Window", DefaultValue = "ByName", ValidValues = ["ByName", "Current"], DisplayValues = ["ByName", "Current Window"], Display = DisplayMode.Native },
            new NamedCalcChild("Name") { PocoProperty = "Name", HrLabel = "Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new NamedCalcChild("Height") { PocoProperty = "Height", HrLabel = "Height", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
            new NamedCalcChild("Width") { PocoProperty = "Width", HrLabel = "Width", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
            new NamedCalcChild("DistanceFromTop") { PocoProperty = "Top", HrLabel = "Top", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
            new NamedCalcChild("DistanceFromLeft") { PocoProperty = "Left", HrLabel = "Left", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
