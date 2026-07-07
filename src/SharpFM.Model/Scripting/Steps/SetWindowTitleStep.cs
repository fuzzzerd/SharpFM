using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetWindowTitleStep : ScriptStep, IStepFactory
{
    public const int XmlId = 124;
    public const string XmlName = "Set Window Title";

    public string Window { get; set; } = "ByName";
    public Calculation OfWindow { get; set; } = new("");
    public bool CurrentFile { get; set; }
    public Calculation NewTitle { get; set; } = new("");

    private SetWindowTitleStep() : base(false) { }

    public SetWindowTitleStep(
        string window = "ByName",
        Calculation? ofWindow = null,
        bool currentFile = true,
        Calculation? newTitle = null,
        bool enabled = true)
        : base(enabled)
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Set Window Title [ " + "Window: " + WindowHr(Window) + " ; " + "Of Window: " + OfWindow.Text + " ; " + "Current file: " + (CurrentFile ? "On" : "Off") + " ; " + "New Title: " + NewTitle.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetWindowTitleStep>(step, Metadata);

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
        // Canonical: LimitToWindowsOfCurrentFile leads, then Window, then the
        // optional <Name> (of-window) and <NewName> (new-title) calculations.
        Shape =
        [
            new BoolStateChild("LimitToWindowsOfCurrentFile") { PocoProperty = "CurrentFile", HrLabel = "Current file", Display = DisplayMode.Augmented },
            new EnumValueChild("Window") { PocoProperty = "Window", HrLabel = "Window", DefaultValue = "ByName", DisplayValues = ["Current Window", "Of Window"], Display = DisplayMode.Native },
            new NamedCalcChild("Name") { PocoProperty = "OfWindow", HrLabel = "Of Window", Optional = true, Display = DisplayMode.Augmented },
            new NamedCalcChild("NewName") { PocoProperty = "NewTitle", HrLabel = "New Title", Optional = true, Display = DisplayMode.Augmented },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
