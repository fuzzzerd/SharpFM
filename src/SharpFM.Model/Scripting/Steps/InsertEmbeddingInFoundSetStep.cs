using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert Embedding in Found Set: The LLMBulkEmbedding parent element
/// carries most params. The Target field is a sibling of LLMBulkEmbedding
/// (outside it); SourceField is a Field element inside LLMBulkEmbedding.
/// Flag elements (Overwrite, ContinueOnError, ShowSummary) use
/// presence = On, absence = Off.
/// </summary>
public sealed class InsertEmbeddingInFoundSetStep : ScriptStep, IStepFactory
{
    public const int XmlId = 216;
    public const string XmlName = "Insert Embedding in Found Set";

    public Calculation AccountName { get; set; }
    public Calculation Model { get; set; }
    public FieldRef? SourceField { get; set; }
    public FieldRef? TargetField { get; set; }
    public bool Overwrite { get; set; }
    public bool ContinueOnError { get; set; }
    public bool ShowSummary { get; set; }
    public Calculation? Parameters { get; set; }

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
        : base(null, enabled)
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

    public override XElement ToXml()
    {
        var bulk = new XElement("LLMBulkEmbedding",
            new XElement("AccountName", AccountName.ToXml("Calculation")),
            new XElement("Model", Model.ToXml("Calculation")));
        if (SourceField is not null) bulk.Add(SourceField.ToXml("Field"));
        if (Overwrite) bulk.Add(new XElement("Overwrite"));
        if (ContinueOnError) bulk.Add(new XElement("ContinueOnError"));
        if (ShowSummary) bulk.Add(new XElement("ShowSummary"));
        if (Parameters is not null) bulk.Add(new XElement("Parameters", Parameters.ToXml("Calculation")));

        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));
        if (TargetField is not null)
        {
            if (TargetField.IsVariable) step.Add(new XElement("Text"));
            step.Add(TargetField.ToXml("Field"));
        }
        step.Add(bulk);
        return step;
    }

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

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var bulk = step.Element("LLMBulkEmbedding");
        var account = bulk?.Element("AccountName")?.Element("Calculation");
        var model = bulk?.Element("Model")?.Element("Calculation");
        var sourceEl = bulk?.Element("Field");
        var source = sourceEl is not null ? FieldRef.FromXml(sourceEl) : null;
        var overwrite = bulk?.Element("Overwrite") is not null;
        var continueErr = bulk?.Element("ContinueOnError") is not null;
        var summary = bulk?.Element("ShowSummary") is not null;
        var paramsEl = bulk?.Element("Parameters")?.Element("Calculation");
        var targetEl = step.Element("Field");
        var target = targetEl is not null ? FieldRef.FromXml(targetEl) : null;
        return new InsertEmbeddingInFoundSetStep(
            account is not null ? Calculation.FromXml(account) : new Calculation(""),
            model is not null ? Calculation.FromXml(model) : new Calculation(""),
            source,
            target,
            overwrite,
            continueErr,
            summary,
            paramsEl is not null ? Calculation.FromXml(paramsEl) : null,
            enabled);
    }

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
