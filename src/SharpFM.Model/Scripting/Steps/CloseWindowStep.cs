using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CloseWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 121;
    public const string XmlName = "Close Window";

    public bool LimitToWindowsOfCurrentFile { get; set; }
    public string Window { get; set; }
    public Calculation Calculation { get; set; }

    public CloseWindowStep(
        bool limitToWindowsOfCurrentFile = true,
        string window = "ByName",
        Calculation? calculation = null,
        bool enabled = true)
        : base(null, enabled)
    {
        LimitToWindowsOfCurrentFile = limitToWindowsOfCurrentFile;
        Window = window;
        Calculation = calculation ?? new Calculation("");
    }

    private static readonly IReadOnlyDictionary<string, string> _WindowToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["ByName"] = "ByName",
        ["Current"] = "Current",
    };
    private static readonly IReadOnlyDictionary<string, string> _WindowFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["ByName"] = "ByName",
        ["Current"] = "Current",
    };
    private static string WindowHr(string x) => _WindowToHr.TryGetValue(x, out var h) ? h : x;
    private static string WindowXml(string h) => _WindowFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("LimitToWindowsOfCurrentFile", new XAttribute("state", LimitToWindowsOfCurrentFile ? "True" : "False")),
            new XElement("Window", new XAttribute("value", Window)),
            new XElement("Name", Calculation.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Close Window [ " + (LimitToWindowsOfCurrentFile ? "On" : "Off") + " ; " + WindowHr(Window) + " ; " + Calculation.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var limitToWindowsOfCurrentFile_v = step.Element("LimitToWindowsOfCurrentFile")?.Attribute("state")?.Value == "True";
        var window_v = step.Element("Window")?.Attribute("value")?.Value ?? "ByName";
        var calculation_vWrapEl = step.Element("Name");
        var calculation_vCalcEl = calculation_vWrapEl?.Element("Calculation");
        var calculation_v = calculation_vCalcEl is not null ? Calculation.FromXml(calculation_vCalcEl) : new Calculation("");
        return new CloseWindowStep(limitToWindowsOfCurrentFile_v, window_v, calculation_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool limitToWindowsOfCurrentFile_v = true;
        string window_v = "ByName";
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(false)) { calculation_v = new Calculation(tok); break; } }
        return new CloseWindowStep(limitToWindowsOfCurrentFile_v, window_v, calculation_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-window.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "LimitToWindowsOfCurrentFile",
                XmlElement = "LimitToWindowsOfCurrentFile",
                Type = "boolean",
                XmlAttr = "state",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
            },
            new ParamMetadata
            {
                Name = "Window",
                XmlElement = "Window",
                Type = "enum",
                XmlAttr = "value",
                ValidValues = ["ByName", "Current"],
                DefaultValue = "ByName",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
