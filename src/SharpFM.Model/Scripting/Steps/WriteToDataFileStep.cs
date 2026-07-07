using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class WriteToDataFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 192;
    public const string XmlName = "Write to Data File";

    public Calculation? FileId { get; set; }
    public FieldRef? DataSource { get; set; }
    public string DataSourceType { get; set; } = "2";
    public bool AppendLineFeed { get; set; }

    private WriteToDataFileStep() : base(false) { }

    public WriteToDataFileStep(
        Calculation? fileId = null,
        FieldRef? dataSource = null,
        string dataSourceType = "2",
        bool appendLineFeed = false,
        bool enabled = true)
        : base(enabled)
    {
        FileId = fileId;
        DataSource = dataSource;
        DataSourceType = dataSourceType;
        AppendLineFeed = appendLineFeed;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: Append line feed is a wire boolean displayed as a bare presence token, and the token order differs from shape order.
    public override string ToDisplayLine()
    {
        var writeAs = DataSourceType switch
        {
            "1" => "UTF-16",
            "2" => "UTF-8",
            _ => DataSourceType,
        };
        var parts = new System.Collections.Generic.List<string> { $"File ID: {FileId?.Text ?? ""}" };
        if (DataSource is not null) parts.Add($"Data source: {DataSource.ToDisplayString()}");
        parts.Add($"Write as: {writeAs}");
        if (AppendLineFeed) parts.Add("Append line feed");
        return $"Write to Data File [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<WriteToDataFileStep>(step, Metadata);

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
        // Canonical: AppendLineFeed, DataSourceType, then the optional bare
        // <Calculation> (file id) and field data source (variable target gets
        // the bare <Text/> marker).
        Shape =
        [
            new BoolStateChild("AppendLineFeed") { PocoProperty = "AppendLineFeed", HrLabel = "Append line feed", Display = DisplayMode.Augmented },
            new EnumValueChild("DataSourceType") { PocoProperty = "DataSourceType", HrLabel = "Write as", DefaultValue = "2", DisplayValues = ["UTF-16", "UTF-8"], Display = DisplayMode.Augmented },
            new BareCalcChild { PocoProperty = "FileId", HrLabel = "File ID", Optional = true, Display = DisplayMode.Native },
            new FieldChild("Field") { PocoProperty = "DataSource", HrLabel = "Data source", Optional = true, VariableTextMarker = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
