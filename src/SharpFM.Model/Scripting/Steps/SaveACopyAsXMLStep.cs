using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveACopyAsXMLStep : ScriptStep, IStepFactory
{
    public const int XmlId = 3;
    public const string XmlName = "Save a Copy as XML";

    public bool IncludeDetailsForAnalysisTools { get; set; }
    public string DestinationFile { get; set; }
    public Calculation WindowName { get; set; }

    public SaveACopyAsXMLStep(
        bool includeDetailsForAnalysisTools = false,
        string destinationFile = "",
        Calculation? windowName = null,
        bool enabled = true)
        : base(null, enabled)
    {
        IncludeDetailsForAnalysisTools = includeDetailsForAnalysisTools;
        DestinationFile = destinationFile;
        WindowName = windowName ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", IncludeDetailsForAnalysisTools ? "True" : "False")),
            new XElement("UniversalPathList", DestinationFile),
            WindowName.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Save a Copy as XML [ " + "Include details for analysis tools: " + (IncludeDetailsForAnalysisTools ? "On" : "Off") + " ; " + "Destination file: " + DestinationFile + " ; " + "Window name: " + WindowName.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var includeDetailsForAnalysisTools_v = step.Element("Option")?.Attribute("state")?.Value == "True";
        var destinationFile_v = step.Element("UniversalPathList")?.Value ?? "";
        var windowName_vEl = step.Element("Calculation");
        var windowName_v = windowName_vEl is not null ? Calculation.FromXml(windowName_vEl) : new Calculation("");
        return new SaveACopyAsXMLStep(includeDetailsForAnalysisTools_v, destinationFile_v, windowName_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool includeDetailsForAnalysisTools_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Include details for analysis tools:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(35).Trim(); includeDetailsForAnalysisTools_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        string destinationFile_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Destination file:", StringComparison.OrdinalIgnoreCase)) { destinationFile_v = tok.Substring(17).Trim(); break; } }
        Calculation? windowName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Window name:", StringComparison.OrdinalIgnoreCase)) { windowName_v = new Calculation(tok.Substring(12).Trim()); break; } }
        return new SaveACopyAsXMLStep(includeDetailsForAnalysisTools_v, destinationFile_v, windowName_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-a-copy-as-xml.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Option",
                XmlElement = "Option",
                Type = "flagBoolean",
                XmlAttr = "state",
                HrLabel = "Include details for analysis tools",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "UniversalPathList",
                XmlElement = "UniversalPathList",
                Type = "text",
                HrLabel = "Destination file",
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
