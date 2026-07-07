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

    private static readonly IReadOnlyDictionary<string, string> _WindowToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["ByName"] = "ByName",
        ["Current"] = "Current Window",
    };
    private static readonly IReadOnlyDictionary<string, string> _WindowFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["ByName"] = "ByName",
        ["Current Window"] = "Current",
    };
    private static string WindowHr(string x) => _WindowToHr.TryGetValue(x, out var h) ? h : x;
    private static string WindowXml(string h) => _WindowFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Move/Resize Window [ " + WindowHr(Window) + " ; " + "Name: " + Name.Text + " ; " + "Current file: " + (CurrentFile ? "On" : "Off") + " ; " + "Height: " + Height.Text + " ; " + "Width: " + Width.Text + " ; " + "Top: " + Top.Text + " ; " + "Left: " + Left.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<MoveResizeWindowStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string window_v = "ByName";
        Calculation? name_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Name:", StringComparison.OrdinalIgnoreCase)) { name_v = new Calculation(tok.Substring(5).Trim()); break; } }
        bool currentFile_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Current file:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); currentFile_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? height_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Height:", StringComparison.OrdinalIgnoreCase)) { height_v = new Calculation(tok.Substring(7).Trim()); break; } }
        Calculation? width_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Width:", StringComparison.OrdinalIgnoreCase)) { width_v = new Calculation(tok.Substring(6).Trim()); break; } }
        Calculation? top_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Top:", StringComparison.OrdinalIgnoreCase)) { top_v = new Calculation(tok.Substring(4).Trim()); break; } }
        Calculation? left_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Left:", StringComparison.OrdinalIgnoreCase)) { left_v = new Calculation(tok.Substring(5).Trim()); break; } }
        return new MoveResizeWindowStep(window_v, name_v, currentFile_v, height_v, width_v, top_v, left_v, enabled);
    }

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
            new BoolStateChild("LimitToWindowsOfCurrentFile") { PocoProperty = "CurrentFile", HrLabel = "Current file", Display = DisplayMode.Augmented },
            new EnumValueChild("Window") { PocoProperty = "Window", DefaultValue = "ByName", DisplayValues = ["ByName", "Current Window"], Display = DisplayMode.Native },
            new NamedCalcChild("Name") { PocoProperty = "Name", HrLabel = "Name", Optional = true, Display = DisplayMode.Augmented },
            new NamedCalcChild("Height") { PocoProperty = "Height", HrLabel = "Height", Optional = true, Display = DisplayMode.Augmented },
            new NamedCalcChild("Width") { PocoProperty = "Width", HrLabel = "Width", Optional = true, Display = DisplayMode.Augmented },
            new NamedCalcChild("DistanceFromTop") { PocoProperty = "Top", HrLabel = "Top", Optional = true, Display = DisplayMode.Augmented },
            new NamedCalcChild("DistanceFromLeft") { PocoProperty = "Left", HrLabel = "Left", Optional = true, Display = DisplayMode.Augmented },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
