using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ReadFromDataFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 193;
    public const string XmlName = "Read from Data File";

    public Calculation FileId { get; set; }
    public Calculation? Count { get; set; }
    public FieldRef? Target { get; set; }
    public string DataSourceType { get; set; }

    public ReadFromDataFileStep(
        Calculation? fileId = null,
        Calculation? count = null,
        FieldRef? target = null,
        string dataSourceType = "3",
        bool enabled = true)
        : base(null, enabled)
    {
        FileId = fileId ?? new Calculation("");
        Count = count;
        Target = target;
        DataSourceType = dataSourceType;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("DataSourceType", new XAttribute("value", DataSourceType)),
            FileId.ToXml("Calculation"));
        if (Target is not null)
        {
            if (Target.IsVariable) step.Add(new XElement("Text"));
            step.Add(Target.ToXml("Field"));
        }
        if (Count is not null)
            step.Add(new XElement("Count", Count.ToXml("Calculation")));
        return step;
    }

    public override string ToDisplayLine()
    {
        var readAs = DataSourceType switch
        {
            "1" => "UTF-16",
            "2" => "UTF-8",
            "3" => "Bytes",
            _ => DataSourceType,
        };
        var amountLabel = DataSourceType == "1" ? "Amount" : "Amount (bytes)";
        var parts = new System.Collections.Generic.List<string> { $"File ID: {FileId.Text}" };
        if (Count is not null) parts.Add($"{amountLabel}: {Count.Text}");
        if (Target is not null) parts.Add($"Target: {Target.ToDisplayString()}");
        parts.Add($"Read as: {readAs}");
        return $"Read from Data File [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var type = step.Element("DataSourceType")?.Attribute("value")?.Value ?? "3";
        var fileIdEl = step.Element("Calculation");
        var fileId = fileIdEl is not null ? Calculation.FromXml(fileIdEl) : new Calculation("");
        var countEl = step.Element("Count")?.Element("Calculation");
        var count = countEl is not null ? Calculation.FromXml(countEl) : null;
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new ReadFromDataFileStep(fileId, count, target, type, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        Calculation fileId = new("");
        Calculation? count = null;
        FieldRef? target = null;
        string type = "3";
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("File ID:", StringComparison.OrdinalIgnoreCase))
                fileId = new Calculation(t.Substring(8).Trim());
            else if (t.StartsWith("Amount (bytes):", StringComparison.OrdinalIgnoreCase))
                count = new Calculation(t.Substring(15).Trim());
            else if (t.StartsWith("Amount:", StringComparison.OrdinalIgnoreCase))
                count = new Calculation(t.Substring(7).Trim());
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            else if (t.StartsWith("Read as:", StringComparison.OrdinalIgnoreCase))
            {
                type = t.Substring(8).Trim() switch
                {
                    "UTF-16" => "1",
                    "UTF-8" => "2",
                    "Bytes" => "3",
                    _ => "3",
                };
            }
        }
        return new ReadFromDataFileStep(fileId, count, target, type, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/read-from-data-file.html",
        Params =
        [
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", HrLabel = "File ID", Required = true },
            new ParamMetadata { Name = "Count", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Amount (bytes)" },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Target" },
            new ParamMetadata { Name = "DataSourceType", XmlElement = "DataSourceType", XmlAttr = "value", Type = "enum", HrLabel = "Read as", ValidValues = ["UTF-16", "UTF-8", "Bytes"], DefaultValue = "3" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
