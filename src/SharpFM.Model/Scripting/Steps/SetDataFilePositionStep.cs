using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetDataFilePositionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 195;
    public const string XmlName = "Set Data File Position";

    public Calculation FileID { get; set; }
    public Calculation NewPosition { get; set; }

    public SetDataFilePositionStep(
        Calculation? fileID = null,
        Calculation? newPosition = null,
        bool enabled = true)
        : base(enabled)
    {
        FileID = fileID ?? new Calculation("");
        NewPosition = newPosition ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            FileID.ToXml("Calculation"),
            new XElement("position", NewPosition.ToXml("Calculation")));

    public override string ToDisplayLine() =>
        "Set Data File Position [ " + "File ID: " + FileID.Text + " ; " + "New position: " + NewPosition.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var fileID_vEl = step.Element("Calculation");
        var fileID_v = fileID_vEl is not null ? Calculation.FromXml(fileID_vEl) : new Calculation("");
        var newPosition_vWrapEl = step.Element("position");
        var newPosition_vCalcEl = newPosition_vWrapEl?.Element("Calculation");
        var newPosition_v = newPosition_vCalcEl is not null ? Calculation.FromXml(newPosition_vCalcEl) : new Calculation("");
        return new SetDataFilePositionStep(fileID_v, newPosition_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? fileID_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("File ID:", StringComparison.OrdinalIgnoreCase)) { fileID_v = new Calculation(tok.Substring(8).Trim()); break; } }
        Calculation? newPosition_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("New position:", StringComparison.OrdinalIgnoreCase)) { newPosition_v = new Calculation(tok.Substring(13).Trim()); break; } }
        return new SetDataFilePositionStep(fileID_v, newPosition_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-data-file-position.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
                HrLabel = "File ID",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "New position",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
