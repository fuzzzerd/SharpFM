using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InsertEmbeddingStep : ScriptStep, IStepFactory
{
    public const int XmlId = 215;
    public const string XmlName = "Insert Embedding";

    public Calculation AccountName { get; set; }
    public Calculation Model { get; set; }
    public Calculation InputText { get; set; }
    public FieldRef? Target { get; set; }

    public InsertEmbeddingStep(
        Calculation? accountName = null,
        Calculation? model = null,
        Calculation? inputText = null,
        FieldRef? target = null,
        bool enabled = true)
        : base(null, enabled)
    {
        AccountName = accountName ?? new Calculation("");
        Model = model ?? new Calculation("");
        InputText = inputText ?? new Calculation("");
        Target = target;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));
        if (Target is not null)
        {
            if (Target.IsVariable) step.Add(new XElement("Text"));
            step.Add(Target.ToXml("Field"));
        }
        step.Add(new XElement("LLMEmbedding",
            new XElement("AccountName", AccountName.ToXml("Calculation")),
            new XElement("Model", Model.ToXml("Calculation")),
            new XElement("InputText", InputText.ToXml("Calculation"))));
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"Account Name: {AccountName.Text}",
            $"Embedding Model: {Model.Text}",
            $"Input: {InputText.Text}",
        };
        if (Target is not null) parts.Add($"Target: {Target.ToDisplayString()}");
        return $"Insert Embedding [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var llm = step.Element("LLMEmbedding");
        var account = llm?.Element("AccountName")?.Element("Calculation");
        var model = llm?.Element("Model")?.Element("Calculation");
        var input = llm?.Element("InputText")?.Element("Calculation");
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new InsertEmbeddingStep(
            account is not null ? Calculation.FromXml(account) : new Calculation(""),
            model is not null ? Calculation.FromXml(model) : new Calculation(""),
            input is not null ? Calculation.FromXml(input) : new Calculation(""),
            target,
            enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        Calculation account = new(""), model = new(""), input = new("");
        FieldRef? target = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase))
                account = new Calculation(t.Substring(13).Trim());
            else if (t.StartsWith("Embedding Model:", StringComparison.OrdinalIgnoreCase))
                model = new Calculation(t.Substring(16).Trim());
            else if (t.StartsWith("Input:", StringComparison.OrdinalIgnoreCase))
                input = new Calculation(t.Substring(6).Trim());
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
        }
        return new InsertEmbeddingStep(account, model, input, target, enabled);
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
            new ParamMetadata { Name = "InputText", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Input", Required = true },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "fieldOrVariable", HrLabel = "Target" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
