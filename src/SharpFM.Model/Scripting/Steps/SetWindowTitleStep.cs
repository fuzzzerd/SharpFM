using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetWindowTitleStep : ScriptStep, IStepFactory
{
    public const int XmlId = 124;
    public const string XmlName = "Set Window Title";

    public string Window { get; set; }
    public Calculation OfWindow { get; set; }
    public bool CurrentFile { get; set; }
    public Calculation NewTitle { get; set; }

    public SetWindowTitleStep(
        string window = "ByName",
        Calculation? ofWindow = null,
        bool currentFile = true,
        Calculation? newTitle = null,
        bool enabled = true)
        : base(null, enabled)
    {
        Window = window;
        OfWindow = ofWindow ?? new Calculation("");
        CurrentFile = currentFile;
        NewTitle = newTitle ?? new Calculation("");
    }

    private static readonly IReadOnlyDictionary<string, string> _WindowToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["Current"] = "Current Window",
        ["ByName"] = "Of Window",
    };
    private static readonly IReadOnlyDictionary<string, string> _WindowFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["Current Window"] = "Current",
        ["Of Window"] = "ByName",
    };
    private static string WindowHr(string x) => _WindowToHr.TryGetValue(x, out var h) ? h : x;
    private static string WindowXml(string h) => _WindowFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Window", new XAttribute("value", Window)),
            new XElement("Name", OfWindow.ToXml("Calculation")),
            new XElement("LimitToWindowsOfCurrentFile", new XAttribute("state", CurrentFile ? "True" : "False")),
            new XElement("NewName", NewTitle.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Set Window Title [ " + "Window: " + WindowHr(Window) + " ; " + "Of Window: " + OfWindow.Text + " ; " + "Current file: " + (CurrentFile ? "On" : "Off") + " ; " + "New Title: " + NewTitle.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var window_v = step.Element("Window")?.Attribute("value")?.Value ?? "ByName";
        var ofWindow_vWrapEl = step.Element("Name");
        var ofWindow_vCalcEl = ofWindow_vWrapEl?.Element("Calculation");
        var ofWindow_v = ofWindow_vCalcEl is not null ? Calculation.FromXml(ofWindow_vCalcEl) : new Calculation("");
        var currentFile_v = step.Element("LimitToWindowsOfCurrentFile")?.Attribute("state")?.Value == "True";
        var newTitle_vWrapEl = step.Element("NewName");
        var newTitle_vCalcEl = newTitle_vWrapEl?.Element("Calculation");
        var newTitle_v = newTitle_vCalcEl is not null ? Calculation.FromXml(newTitle_vCalcEl) : new Calculation("");
        return new SetWindowTitleStep(window_v, ofWindow_v, currentFile_v, newTitle_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string window_v = "ByName";
        foreach (var tok in tokens) { if (tok.StartsWith("Window:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); window_v = WindowXml(v); break; } }
        Calculation? ofWindow_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Of Window:", StringComparison.OrdinalIgnoreCase)) { ofWindow_v = new Calculation(tok.Substring(10).Trim()); break; } }
        bool currentFile_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Current file:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); currentFile_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? newTitle_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("New Title:", StringComparison.OrdinalIgnoreCase)) { newTitle_v = new Calculation(tok.Substring(10).Trim()); break; } }
        return new SetWindowTitleStep(window_v, ofWindow_v, currentFile_v, newTitle_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-window-title.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Window",
                XmlElement = "Window",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Window",
                ValidValues = ["Current Window", "Of Window"],
                DefaultValue = "ByName",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Of Window",
            },
            new ParamMetadata
            {
                Name = "LimitToWindowsOfCurrentFile",
                XmlElement = "LimitToWindowsOfCurrentFile",
                Type = "flagBoolean",
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
                HrLabel = "New Title",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
