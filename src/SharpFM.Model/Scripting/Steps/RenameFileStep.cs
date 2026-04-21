using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RenameFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 199;
    public const string XmlName = "Rename File";

    public string SourceFile { get; set; }
    public Calculation NewName { get; set; }

    public RenameFileStep(
        string sourceFile = "",
        Calculation? newName = null,
        bool enabled = true)
        : base(enabled)
    {
        SourceFile = sourceFile;
        NewName = newName ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("UniversalPathList", SourceFile),
            NewName.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Rename File [ " + "Source file: " + SourceFile + " ; " + "New name: " + NewName.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var sourceFile_v = step.Element("UniversalPathList")?.Value ?? "";
        var newName_vEl = step.Element("Calculation");
        var newName_v = newName_vEl is not null ? Calculation.FromXml(newName_vEl) : new Calculation("");
        return new RenameFileStep(sourceFile_v, newName_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string sourceFile_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("Source file:", StringComparison.OrdinalIgnoreCase)) { sourceFile_v = tok.Substring(12).Trim(); break; } }
        Calculation? newName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("New name:", StringComparison.OrdinalIgnoreCase)) { newName_v = new Calculation(tok.Substring(9).Trim()); break; } }
        return new RenameFileStep(sourceFile_v, newName_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/rename-file.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "UniversalPathList",
                XmlElement = "UniversalPathList",
                Type = "text",
                HrLabel = "Source file",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
                HrLabel = "New name",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
