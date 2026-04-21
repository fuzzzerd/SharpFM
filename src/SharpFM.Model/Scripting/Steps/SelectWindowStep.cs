using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SelectWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 123;
    public const string XmlName = "Select Window";

    public bool CurrentFile { get; set; }
    public string Window { get; set; }
    public Calculation Name { get; set; }

    public SelectWindowStep(
        bool currentFile = true,
        string window = "ByName",
        Calculation? name = null,
        bool enabled = true)
        : base(null, enabled)
    {
        CurrentFile = currentFile;
        Window = window;
        Name = name ?? new Calculation("");
    }

    private static readonly IReadOnlyDictionary<string, string> _WindowToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["ByName"] = "Name: <calc>",
        ["Current"] = "Current Window",
    };
    private static readonly IReadOnlyDictionary<string, string> _WindowFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["Name: <calc>"] = "ByName",
        ["Current Window"] = "Current",
    };
    private static string WindowHr(string x) => _WindowToHr.TryGetValue(x, out var h) ? h : x;
    private static string WindowXml(string h) => _WindowFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("LimitToWindowsOfCurrentFile", new XAttribute("state", CurrentFile ? "True" : "False")),
            new XElement("Window", new XAttribute("value", Window)),
            new XElement("Name", Name.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Select Window [ " + "Current file: " + (CurrentFile ? "On" : "Off") + " ; " + "Window: " + WindowHr(Window) + " ; " + "Name: " + Name.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var currentFile_v = step.Element("LimitToWindowsOfCurrentFile")?.Attribute("state")?.Value == "True";
        var window_v = step.Element("Window")?.Attribute("value")?.Value ?? "ByName";
        var name_vWrapEl = step.Element("Name");
        var name_vCalcEl = name_vWrapEl?.Element("Calculation");
        var name_v = name_vCalcEl is not null ? Calculation.FromXml(name_vCalcEl) : new Calculation("");
        return new SelectWindowStep(currentFile_v, window_v, name_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool currentFile_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Current file:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(13).Trim(); currentFile_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        string window_v = "ByName";
        foreach (var tok in tokens) { if (tok.StartsWith("Window:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); window_v = WindowXml(v); break; } }
        Calculation? name_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Name:", StringComparison.OrdinalIgnoreCase)) { name_v = new Calculation(tok.Substring(5).Trim()); break; } }
        return new SelectWindowStep(currentFile_v, window_v, name_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/select-window.html",
        Params =
        [
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
                Name = "Window",
                XmlElement = "Window",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Window",
                ValidValues = ["Name: <calc>", "Current Window"],
                DefaultValue = "ByName",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Name",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
