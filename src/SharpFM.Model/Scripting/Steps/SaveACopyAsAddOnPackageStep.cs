using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveACopyAsAddOnPackageStep : ScriptStep, IStepFactory
{
    public const int XmlId = 96;
    public const string XmlName = "Save a Copy as Add-on Package";

    public bool ReplaceUUIDs { get; set; }
    public Calculation WindowName { get; set; } = new("");

    private SaveACopyAsAddOnPackageStep() : base(false) { }

    public SaveACopyAsAddOnPackageStep(
        bool replaceUUIDs = false,
        Calculation? windowName = null,
        bool enabled = true)
        : base(enabled)
    {
        ReplaceUUIDs = replaceUUIDs;
        WindowName = windowName ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Save a Copy as Add-on Package [ " + "Replace UUIDs: " + (ReplaceUUIDs ? "On" : "Off") + " ; " + "Window name: " + WindowName.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SaveACopyAsAddOnPackageStep>(step, Metadata);

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
        // Canonical: LinkAvail then the window-name Calculation, which the
        // unconfigured form omits (Optional).
        Shape =
        [
            new BoolStateChild("LinkAvail") { PocoProperty = "ReplaceUUIDs", HrLabel = "Replace UUIDs", Display = DisplayMode.Hidden },
            new BareCalcChild { PocoProperty = "WindowName", HrLabel = "Window name", Optional = true, Display = DisplayMode.Native },
        ],
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
