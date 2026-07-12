using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CloseWindowStep : ScriptStep<CloseWindowStep>, IStepFactory
{
    public const int XmlId = 121;
    public const string XmlName = "Close Window";

    public bool LimitToWindowsOfCurrentFile { get; set; }
    public string Window { get; set; } = "ByName";
    public Calculation Calculation { get; set; } = new("");

    private CloseWindowStep() : base(false) { }

    public CloseWindowStep(
        bool limitToWindowsOfCurrentFile = true,
        string window = "ByName",
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
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

    // Hand-written: variant window-target grammar (Current/ByName) the shape
    // renderer cannot produce.
    public override string ToDisplayLine() =>
        "Close Window [ " + (LimitToWindowsOfCurrentFile ? "On" : "Off") + " ; " + WindowHr(Window) + " ; " + Calculation.Text + " ]";

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        // Positional display grammar: [ On/Off ; Window ; name-calc ]. A
        // trailing empty calc token is dropped by the param splitter.
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool limitToWindowsOfCurrentFile_v = tokens.Length == 0
            || tokens[0].Equals("On", StringComparison.OrdinalIgnoreCase);
        string window_v = tokens.Length > 1 ? WindowXml(tokens[1]) : "ByName";
        Calculation? calculation_v = tokens.Length > 2 && tokens[2].Length > 0
            ? new Calculation(tokens[2])
            : null;
        LimitToWindowsOfCurrentFile = limitToWindowsOfCurrentFile_v;
        Window = window_v;
        Calculation = calculation_v ?? new Calculation("");
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-window.html",
        // Canonical: LimitToWindowsOfCurrentFile, Window, then the optional
        // <Name> calculation (present only in the ByName form).
        Shape =
        [
            new BoolStateChild("LimitToWindowsOfCurrentFile") { PocoProperty = "LimitToWindowsOfCurrentFile", Display = DisplayMode.Native },
            new EnumValueChild("Window") { PocoProperty = "Window", DefaultValue = "ByName", DisplayValues = ["ByName", "Current"], Display = DisplayMode.Native },
            new NamedCalcChild("Name") { PocoProperty = "Calculation", Optional = true, Display = DisplayMode.Native },
        ],
    };
}
