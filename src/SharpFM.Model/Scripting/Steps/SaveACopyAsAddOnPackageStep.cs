using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveACopyAsAddOnPackageStep : ScriptStep, IStepFactory
{
    public const int XmlId = 96;
    public const string XmlName = "Save a Copy as Add-on Package";

    public bool ReplaceUUIDs { get; set; }
    public Calculation WindowName { get; set; }

    public SaveACopyAsAddOnPackageStep(
        bool replaceUUIDs = false,
        Calculation? windowName = null,
        bool enabled = true)
        : base(enabled)
    {
        ReplaceUUIDs = replaceUUIDs;
        WindowName = windowName ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("LinkAvail", new XAttribute("state", ReplaceUUIDs ? "True" : "False")),
            WindowName.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Save a Copy as Add-on Package [ " + "Replace UUIDs: " + (ReplaceUUIDs ? "On" : "Off") + " ; " + "Window name: " + WindowName.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var replaceUUIDs_v = step.Element("LinkAvail")?.Attribute("state")?.Value == "True";
        var windowName_vEl = step.Element("Calculation");
        var windowName_v = windowName_vEl is not null ? Calculation.FromXml(windowName_vEl) : new Calculation("");
        return new SaveACopyAsAddOnPackageStep(replaceUUIDs_v, windowName_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool replaceUUIDs_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Replace UUIDs:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(14).Trim(); replaceUUIDs_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? windowName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Window name:", StringComparison.OrdinalIgnoreCase)) { windowName_v = new Calculation(tok.Substring(12).Trim()); break; } }
        return new SaveACopyAsAddOnPackageStep(replaceUUIDs_v, windowName_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-a-copy-as-add-on-package.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "LinkAvail",
                XmlElement = "LinkAvail",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Replace UUIDs",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
                HrLabel = "Window name",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
