using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class WriteToDataFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 192;
    public const string XmlName = "Write to Data File";

    public Calculation FileId { get; set; }
    public FieldRef? DataSource { get; set; }
    public string DataSourceType { get; set; }
    public bool AppendLineFeed { get; set; }

    public WriteToDataFileStep(
        Calculation? fileId = null,
        FieldRef? dataSource = null,
        string dataSourceType = "2",
        bool appendLineFeed = false,
        bool enabled = true)
        : base(enabled)
    {
        FileId = fileId ?? new Calculation("");
        DataSource = dataSource;
        DataSourceType = dataSourceType;
        AppendLineFeed = appendLineFeed;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("AppendLineFeed", new XAttribute("state", AppendLineFeed ? "True" : "False")),
            new XElement("DataSourceType", new XAttribute("value", DataSourceType)),
            FileId.ToXml("Calculation"));
        if (DataSource is not null)
        {
            if (DataSource.IsVariable) step.Add(new XElement("Text"));
            step.Add(DataSource.ToXml("Field"));
        }
        return step;
    }

    public override string ToDisplayLine()
    {
        var writeAs = DataSourceType switch
        {
            "1" => "UTF-16",
            "2" => "UTF-8",
            _ => DataSourceType,
        };
        var parts = new System.Collections.Generic.List<string> { $"File ID: {FileId.Text}" };
        if (DataSource is not null) parts.Add($"Data source: {DataSource.ToDisplayString()}");
        parts.Add($"Write as: {writeAs}");
        if (AppendLineFeed) parts.Add("Append line feed");
        return $"Write to Data File [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var append = step.Element("AppendLineFeed")?.Attribute("state")?.Value == "True";
        var type = step.Element("DataSourceType")?.Attribute("value")?.Value ?? "2";
        var fileIdEl = step.Element("Calculation");
        var fileId = fileIdEl is not null ? Calculation.FromXml(fileIdEl) : new Calculation("");
        var fieldEl = step.Element("Field");
        var field = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new WriteToDataFileStep(fileId, field, type, append, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        Calculation fileId = new("");
        FieldRef? source = null;
        string type = "2";
        bool append = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("File ID:", StringComparison.OrdinalIgnoreCase))
                fileId = new Calculation(t.Substring(8).Trim());
            else if (t.StartsWith("Data source:", StringComparison.OrdinalIgnoreCase))
                source = FieldRef.FromDisplayToken(t.Substring(12).Trim());
            else if (t.StartsWith("Write as:", StringComparison.OrdinalIgnoreCase))
            {
                type = t.Substring(9).Trim() switch
                {
                    "UTF-16" => "1",
                    "UTF-8" => "2",
                    _ => "2",
                };
            }
            else if (t.Equals("Append line feed", StringComparison.OrdinalIgnoreCase))
                append = true;
        }
        return new WriteToDataFileStep(fileId, source, type, append, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/write-to-data-file.html",
        Params =
        [
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", HrLabel = "File ID", Required = true },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Data source" },
            new ParamMetadata { Name = "DataSourceType", XmlElement = "DataSourceType", XmlAttr = "value", Type = "enum", HrLabel = "Write as", ValidValues = ["UTF-16", "UTF-8"], DefaultValue = "2" },
            new ParamMetadata { Name = "AppendLineFeed", XmlElement = "AppendLineFeed", XmlAttr = "state", Type = "flagBoolean", HrLabel = "Append line feed" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
