using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ReadFromDataFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 193;
    public const string XmlName = "Read from Data File";

    public Calculation? FileId { get; set; }
    public Calculation? Count { get; set; }
    public FieldRef? Target { get; set; }
    public string DataSourceType { get; set; } = "3";

    private ReadFromDataFileStep() : base(false) { }

    public ReadFromDataFileStep(
        Calculation? fileId = null,
        Calculation? count = null,
        FieldRef? target = null,
        string dataSourceType = "3",
        bool enabled = true)
        : base(enabled)
    {
        FileId = fileId;
        Count = count;
        Target = target;
        DataSourceType = dataSourceType;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: the amount label is per-mode conditional ("Amount" for
    // UTF-16, "Amount (bytes)" otherwise) — a form the shape renderer's static
    // HrLabel cannot express.
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
        var parts = new System.Collections.Generic.List<string> { $"File ID: {FileId?.Text ?? ""}" };
        if (Count is not null) parts.Add($"{amountLabel}: {Count.Text}");
        if (Target is not null) parts.Add($"Target: {Target.ToDisplayString()}");
        parts.Add($"Read as: {readAs}");
        return $"Read from Data File [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ReadFromDataFileStep>(step, Metadata);

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
        // Canonical: DataSourceType, then the optional bare <Calculation> (file
        // id), field target (variable target gets the bare <Text/> marker), and
        // the <Count> amount calculation.
        Shape =
        [
            new EnumValueChild("DataSourceType") { PocoProperty = "DataSourceType", HrLabel = "Read as", DefaultValue = "3", DisplayValues = ["UTF-16", "UTF-8", "Bytes"], Display = DisplayMode.Augmented },
            new BareCalcChild { PocoProperty = "FileId", HrLabel = "File ID", Optional = true, Display = DisplayMode.Native },
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Target", Optional = true, VariableTextMarker = true, Display = DisplayMode.Native },
            new NamedCalcChild("Count") { PocoProperty = "Count", HrLabel = "Amount (bytes)", Optional = true, Display = DisplayMode.Augmented },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
