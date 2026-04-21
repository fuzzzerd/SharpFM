using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class MoveResizeWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 119;
    public const string XmlName = "Move/Resize Window";

    public string Window { get; set; }
    public Calculation Name { get; set; }
    public bool CurrentFile { get; set; }
    public Calculation Height { get; set; }
    public Calculation Width { get; set; }
    public Calculation Top { get; set; }
    public Calculation Left { get; set; }

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

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Window", new XAttribute("value", Window)),
            new XElement("Name", Name.ToXml("Calculation")),
            new XElement("LimitToWindowsOfCurrentFile", new XAttribute("state", CurrentFile ? "True" : "False")),
            new XElement("Height", Height.ToXml("Calculation")),
            new XElement("Width", Width.ToXml("Calculation")),
            new XElement("DistanceFromTop", Top.ToXml("Calculation")),
            new XElement("DistanceFromLeft", Left.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Move/Resize Window [ " + WindowHr(Window) + " ; " + "Name: " + Name.Text + " ; " + "Current file: " + (CurrentFile ? "On" : "Off") + " ; " + "Height: " + Height.Text + " ; " + "Width: " + Width.Text + " ; " + "Top: " + Top.Text + " ; " + "Left: " + Left.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var window_v = step.Element("Window")?.Attribute("value")?.Value ?? "ByName";
        var name_vWrapEl = step.Element("Name");
        var name_vCalcEl = name_vWrapEl?.Element("Calculation");
        var name_v = name_vCalcEl is not null ? Calculation.FromXml(name_vCalcEl) : new Calculation("");
        var currentFile_v = step.Element("LimitToWindowsOfCurrentFile")?.Attribute("state")?.Value == "True";
        var height_vWrapEl = step.Element("Height");
        var height_vCalcEl = height_vWrapEl?.Element("Calculation");
        var height_v = height_vCalcEl is not null ? Calculation.FromXml(height_vCalcEl) : new Calculation("");
        var width_vWrapEl = step.Element("Width");
        var width_vCalcEl = width_vWrapEl?.Element("Calculation");
        var width_v = width_vCalcEl is not null ? Calculation.FromXml(width_vCalcEl) : new Calculation("");
        var top_vWrapEl = step.Element("DistanceFromTop");
        var top_vCalcEl = top_vWrapEl?.Element("Calculation");
        var top_v = top_vCalcEl is not null ? Calculation.FromXml(top_vCalcEl) : new Calculation("");
        var left_vWrapEl = step.Element("DistanceFromLeft");
        var left_vCalcEl = left_vWrapEl?.Element("Calculation");
        var left_v = left_vCalcEl is not null ? Calculation.FromXml(left_vCalcEl) : new Calculation("");
        return new MoveResizeWindowStep(window_v, name_v, currentFile_v, height_v, width_v, top_v, left_v, enabled);
    }

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
        Params =
        [
            new ParamMetadata
            {
                Name = "Window",
                XmlElement = "Window",
                Type = "enum",
                XmlAttr = "value",
                ValidValues = ["ByName", "Current Window"],
                DefaultValue = "ByName",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Name",
            },
            new ParamMetadata
            {
                Name = "LimitToWindowsOfCurrentFile",
                XmlElement = "LimitToWindowsOfCurrentFile",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Current file",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Height",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Width",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Top",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Left",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
