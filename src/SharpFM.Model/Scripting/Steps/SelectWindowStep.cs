using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SelectWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 123;
    public const string XmlName = "Select Window";

    public bool CurrentFile { get; set; }
    public string Window { get; set; } = "ByName";
    public Calculation Name { get; set; } = new("");

    private SelectWindowStep() : base(false) { }

    public SelectWindowStep(
        bool currentFile = true,
        string window = "ByName",
        Calculation? name = null,
        bool enabled = true)
        : base(enabled)
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Select Window [ " + "Current file: " + (CurrentFile ? "On" : "Off") + " ; " + "Window: " + WindowHr(Window) + " ; " + "Name: " + Name.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SelectWindowStep>(step, Metadata);

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
        // Canonical: LimitToWindowsOfCurrentFile, Window, then the optional
        // <Name> calculation (present only in the ByName form).
        Shape =
        [
            new BoolStateChild("LimitToWindowsOfCurrentFile") { PocoProperty = "CurrentFile", HrLabel = "Current file", Display = DisplayMode.Native },
            new EnumValueChild("Window") { PocoProperty = "Window", HrLabel = "Window", DefaultValue = "ByName", DisplayValues = ["Name: <calc>", "Current Window"], Display = DisplayMode.Native },
            new NamedCalcChild("Name") { PocoProperty = "Name", HrLabel = "Name", Optional = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
