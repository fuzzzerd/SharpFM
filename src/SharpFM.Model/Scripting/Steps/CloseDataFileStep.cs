using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class CloseDataFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 196;
    public const string XmlName = "Close Data File";

    public Calculation FileID { get; set; }

    public CloseDataFileStep(
        Calculation? fileID = null,
        bool enabled = true)
        : base(null, enabled)
    {
        FileID = fileID ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            FileID.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Close Data File [ " + "File ID: " + FileID.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var fileID_vEl = step.Element("Calculation");
        var fileID_v = fileID_vEl is not null ? Calculation.FromXml(fileID_vEl) : new Calculation("");
        return new CloseDataFileStep(fileID_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? fileID_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("File ID:", StringComparison.OrdinalIgnoreCase)) { fileID_v = new Calculation(tok.Substring(8).Trim()); break; } }
        return new CloseDataFileStep(fileID_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/close-data-file.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
                HrLabel = "File ID",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
