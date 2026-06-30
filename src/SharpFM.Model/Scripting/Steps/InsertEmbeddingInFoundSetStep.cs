using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert Embedding in Found Set (216). Canonical form (skill, AI reference): an
/// optional target <c>Field</c> sibling, then an <c>&lt;LLMBulkEmbedding&gt;</c>
/// wrapper holding the optional account/model calcs, the source <c>Field</c>,
/// the presence-flag options (Overwrite / ContinueOnError / ShowSummary) and the
/// optional parameters calc. Emitted as an empty wrapper when unconfigured.
/// </summary>
public sealed class InsertEmbeddingInFoundSetStep : ScriptStep, IStepFactory
{
    public const int XmlId = 216;
    public const string XmlName = "Insert Embedding in Found Set";

    public Calculation AccountName { get; set; } = new("");
    public Calculation Model { get; set; } = new("");
    public FieldRef? SourceField { get; set; }
    public FieldRef? TargetField { get; set; }
    public bool Overwrite { get; set; }
    public bool ContinueOnError { get; set; }
    public bool ShowSummary { get; set; }
    public Calculation? Parameters { get; set; }

    private InsertEmbeddingInFoundSetStep() : base(false) { }

    public InsertEmbeddingInFoundSetStep(
        Calculation? accountName = null,
        Calculation? model = null,
        FieldRef? sourceField = null,
        FieldRef? targetField = null,
        bool overwrite = false,
        bool continueOnError = false,
        bool showSummary = false,
        Calculation? parameters = null,
        bool enabled = true)
        : base(enabled)
    {
        AccountName = accountName ?? new Calculation("");
        Model = model ?? new Calculation("");
        SourceField = sourceField;
        TargetField = targetField;
        Overwrite = overwrite;
        ContinueOnError = continueOnError;
        ShowSummary = showSummary;
        Parameters = parameters;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"Account Name: {AccountName.Text}",
            $"Embedding Model: {Model.Text}",
        };
        if (SourceField is not null) parts.Add($"Source Field: {SourceField.ToDisplayString()}");
        if (TargetField is not null) parts.Add($"Target Field: {TargetField.ToDisplayString()}");
        if (Overwrite) parts.Add("Replace target contents");
        if (ContinueOnError) parts.Add("Continue on error");
        if (ShowSummary) parts.Add("Show summary");
        if (Parameters is not null) parts.Add($"Parameters: {Parameters.Text}");
        return $"Insert Embedding in Found Set [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertEmbeddingInFoundSetStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        Calculation account = new(""), model = new("");
        FieldRef? source = null, target = null;
        bool overwrite = false, continueErr = false, summary = false;
        Calculation? parameters = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase))
                account = new Calculation(t.Substring(13).Trim());
            else if (t.StartsWith("Embedding Model:", StringComparison.OrdinalIgnoreCase))
                model = new Calculation(t.Substring(16).Trim());
            else if (t.StartsWith("Source Field:", StringComparison.OrdinalIgnoreCase))
                source = FieldRef.FromDisplayToken(t.Substring(13).Trim());
            else if (t.StartsWith("Target Field:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(13).Trim());
            else if (t.Equals("Replace target contents", StringComparison.OrdinalIgnoreCase))
                overwrite = true;
            else if (t.Equals("Continue on error", StringComparison.OrdinalIgnoreCase))
                continueErr = true;
            else if (t.Equals("Show summary", StringComparison.OrdinalIgnoreCase))
                summary = true;
            else if (t.StartsWith("Parameters:", StringComparison.OrdinalIgnoreCase))
                parameters = new Calculation(t.Substring(11).Trim());
        }
        return new InsertEmbeddingInFoundSetStep(account, model, source, target, overwrite, continueErr, summary, parameters, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        // Canonical: optional target Field sibling, then the <LLMBulkEmbedding>
        // wrapper with optional account/model, source Field, presence-flag
        // options, and the optional parameters calc.
        Shape =
        [
            new FieldChild("Field") { PocoProperty = "TargetField", Optional = true, VariableTextMarker = true, Display = DisplayMode.Native },
            new WrapperChild("LLMBulkEmbedding",
            [
                new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Augmented },
                new NamedCalcChild("Model") { PocoProperty = "Model", HrLabel = "Embedding Model", Optional = true, Display = DisplayMode.Augmented },
                new FieldChild("Field") { PocoProperty = "SourceField", HrLabel = "Source Field", Optional = true, Display = DisplayMode.Native },
                new FlagChild("Overwrite") { PocoProperty = "Overwrite", HrLabel = "Replace target contents" },
                new FlagChild("ContinueOnError") { PocoProperty = "ContinueOnError", HrLabel = "Continue on error" },
                new FlagChild("ShowSummary") { PocoProperty = "ShowSummary", HrLabel = "Show summary" },
                new NamedCalcChild("Parameters") { PocoProperty = "Parameters", HrLabel = "Parameters", Optional = true, Display = DisplayMode.Augmented },
            ]),
        ],
        Params =
        [
            new ParamMetadata { Name = "AccountName", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Account Name", Required = true },
            new ParamMetadata { Name = "Model", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Embedding Model", Required = true },
            new ParamMetadata { Name = "SourceField", XmlElement = "Field", Type = "field", HrLabel = "Source Field" },
            new ParamMetadata { Name = "TargetField", XmlElement = "Field", Type = "field", HrLabel = "Target Field" },
            new ParamMetadata { Name = "Overwrite", XmlElement = "Overwrite", Type = "flagElement", HrLabel = "Replace target contents" },
            new ParamMetadata { Name = "ContinueOnError", XmlElement = "ContinueOnError", Type = "flagElement", HrLabel = "Continue on error" },
            new ParamMetadata { Name = "ShowSummary", XmlElement = "ShowSummary", Type = "flagElement", HrLabel = "Show summary" },
            new ParamMetadata { Name = "Parameters", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Parameters" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
