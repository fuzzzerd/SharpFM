using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ShowHideToolbarsStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus one child element per metadata param.
/// Display form uses labeled segments joined by ' ; '; FromDisplayParams
/// scans tokens by label so segment order is free.
/// </summary>
public sealed class ShowHideToolbarsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 29;
    public const string XmlName = "Show/Hide Toolbars";

    public bool IncludeEditRecordToolbar { get; set; }
    public bool Lock { get; set; }
    public string Action { get; set; }

    private ShowHideToolbarsStep() : base(false)
    {
        Action = "Hide";
    }

    public ShowHideToolbarsStep(
        bool includeEditRecordToolbar = false,
        bool @lock = false,
        string action = "Hide",
        bool enabled = true)
        : base(enabled)
    {
        IncludeEditRecordToolbar = includeEditRecordToolbar;
        Lock = @lock;
        Action = action;
    }

    private static readonly IReadOnlyDictionary<string, string> _ActionXmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Show"] = "Show",
        ["Hide"] = "Hide",
        ["Toggle"] = "Toggle",
    };

    private static readonly IReadOnlyDictionary<string, string> _ActionHrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Show"] = "Show",
        ["Hide"] = "Hide",
        ["Toggle"] = "Toggle",
    };

    private static string ActionToHr(string x) =>
        _ActionXmlToHr.TryGetValue(x, out var h) ? h : x;

    private static string ActionFromHr(string h) =>
        _ActionHrToXml.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Show/Hide Toolbars [ " + "Include Edit Record Toolbar: " + (IncludeEditRecordToolbar ? "On" : "Off") + " ; " + "Lock: " + (Lock ? "On" : "Off") + " ; " + "Action: " + ActionToHr(Action) + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ShowHideToolbarsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool includeEditRecordToolbar_val = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Include Edit Record Toolbar:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(28).Trim(); includeEditRecordToolbar_val = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        bool @lock_val = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Lock:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(5).Trim(); @lock_val = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        string action_val = "Hide";
        foreach (var tok in tokens) { if (tok.StartsWith("Action:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); action_val = ActionFromHr(v); break; } }
        return new ShowHideToolbarsStep(includeEditRecordToolbar_val, @lock_val, action_val, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/show-hide-toolbars.html",
        Shape =
        [
            new BoolStateChild("IncludeEditRecordToolbar") { HrLabel = "Include Edit Record Toolbar" },
            new BoolStateChild("Lock") { HrLabel = "Lock" },
            new EnumValueChild("ShowHide") { PocoProperty = "Action", HrLabel = "Action", ValidValues = ["Show", "Hide", "Toggle"], DefaultValue = "Hide" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
