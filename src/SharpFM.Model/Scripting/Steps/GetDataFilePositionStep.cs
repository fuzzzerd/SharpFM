using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GetDataFilePositionStep : ScriptStep, IStepFactory
{
    public const int XmlId = 194;
    public const string XmlName = "Get Data File Position";

    public Calculation FileId { get; set; }
    public FieldRef? Target { get; set; }

    public GetDataFilePositionStep(Calculation? fileId = null, FieldRef? target = null, bool enabled = true)
        : base(enabled)
    {
        FileId = fileId ?? new Calculation("");
        Target = target;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            FileId.ToXml("Calculation"));
        if (Target is not null) step.Add(Target.ToXml("Field"));
        return step;
    }

    public override string ToDisplayLine() =>
        Target is null
            ? $"Get Data File Position [ File ID: {FileId.Text} ]"
            : $"Get Data File Position [ File ID: {FileId.Text} ; Target: {Target.ToDisplayString()} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var calcEl = step.Element("Calculation");
        var fileId = calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new GetDataFilePositionStep(fileId, target, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        Calculation fileId = new("");
        FieldRef? target = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("File ID:", StringComparison.OrdinalIgnoreCase))
            {
                fileId = new Calculation(t.Substring(8).Trim());
            }
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
            {
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            }
        }
        return new GetDataFilePositionStep(fileId, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/get-data-file-position.html",
        Params =
        [
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", HrLabel = "File ID", Required = true },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Target" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
