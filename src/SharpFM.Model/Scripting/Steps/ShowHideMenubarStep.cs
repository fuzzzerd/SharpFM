using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ShowHideMenubarStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus one child element per metadata param.
/// Display form uses labeled segments joined by ' ; '; FromDisplayParams
/// scans tokens by label so segment order is free.
/// </summary>
public sealed class ShowHideMenubarStep : ScriptStep, IStepFactory
{
    public const int XmlId = 166;
    public const string XmlName = "Show/Hide Menubar";

    public bool Lock { get; set; }
    public string Action { get; set; }

    public ShowHideMenubarStep(
        bool @lock = false,
        string action = "Hide",
        bool enabled = true)
        : base(enabled)
    {
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

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Lock", new XAttribute("state", Lock ? "True" : "False")),
            new XElement("ShowHide", new XAttribute("value", Action)));

    public override string ToDisplayLine() =>
        "Show/Hide Menubar [ " + "Lock: " + (Lock ? "On" : "Off") + " ; " + "Action: " + ActionToHr(Action) + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var @lock_val = step.Element("Lock")?.Attribute("state")?.Value == "True";
        var action_val = step.Element("ShowHide")?.Attribute("value")?.Value ?? "";
        return new ShowHideMenubarStep(@lock_val, action_val, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool @lock_val = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Lock:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(5).Trim(); @lock_val = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        string action_val = "Hide";
        foreach (var tok in tokens) { if (tok.StartsWith("Action:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); action_val = ActionFromHr(v); break; } }
        return new ShowHideMenubarStep(@lock_val, action_val, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/show-hide-menubar.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Lock",
                XmlElement = "Lock",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Lock",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "ShowHide",
                XmlElement = "ShowHide",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Action",
                ValidValues = ["Show", "Hide", "Toggle"],
                DefaultValue = "Hide",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
