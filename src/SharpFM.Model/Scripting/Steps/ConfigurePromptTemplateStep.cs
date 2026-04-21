using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ConfigurePromptTemplateStep : ScriptStep, IStepFactory
{
    public const int XmlId = 226;
    public const string XmlName = "Configure Prompt Template";

    public Calculation TemplateName { get; set; }
    public string ModelProvider { get; set; }
    public string TemplateType { get; set; }
    public Calculation SQLPrompt { get; set; }
    public Calculation NaturalLanguagePrompt { get; set; }
    public Calculation FindRequestPrompt { get; set; }
    public Calculation RAGPrompt { get; set; }
    public bool Option { get; set; }

    public ConfigurePromptTemplateStep(
        Calculation? templateName = null,
        string modelProvider = "OpenAI",
        string templateType = "SQL Query",
        Calculation? sQLPrompt = null,
        Calculation? naturalLanguagePrompt = null,
        Calculation? findRequestPrompt = null,
        Calculation? rAGPrompt = null,
        bool option = false,
        bool enabled = true)
        : base(enabled)
    {
        TemplateName = templateName ?? new Calculation("");
        ModelProvider = modelProvider;
        TemplateType = templateType;
        SQLPrompt = sQLPrompt ?? new Calculation("");
        NaturalLanguagePrompt = naturalLanguagePrompt ?? new Calculation("");
        FindRequestPrompt = findRequestPrompt ?? new Calculation("");
        RAGPrompt = rAGPrompt ?? new Calculation("");
        Option = option;
    }

    private static readonly IReadOnlyDictionary<string, string> _ModelProviderToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["OpenAI"] = "OpenAI",
        ["Anthropic"] = "Anthropic",
        ["Cohere"] = "Cohere",
        ["Custom"] = "Custom",
    };
    private static readonly IReadOnlyDictionary<string, string> _ModelProviderFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["OpenAI"] = "OpenAI",
        ["Anthropic"] = "Anthropic",
        ["Cohere"] = "Cohere",
        ["Custom"] = "Custom",
    };
    private static string ModelProviderHr(string x) => _ModelProviderToHr.TryGetValue(x, out var h) ? h : x;
    private static string ModelProviderXml(string h) => _ModelProviderFromHr.TryGetValue(h, out var x) ? x : h;

    private static readonly IReadOnlyDictionary<string, string> _TemplateTypeToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["SQL Query"] = "SQL Query",
        ["Find Request"] = "Find Request",
        ["RAG Prompt"] = "RAG Prompt",
    };
    private static readonly IReadOnlyDictionary<string, string> _TemplateTypeFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["SQL Query"] = "SQL Query",
        ["Find Request"] = "Find Request",
        ["RAG Prompt"] = "RAG Prompt",
    };
    private static string TemplateTypeHr(string x) => _TemplateTypeToHr.TryGetValue(x, out var h) ? h : x;
    private static string TemplateTypeXml(string h) => _TemplateTypeFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("TemplateName", TemplateName.ToXml("Calculation")),
            new XElement("ModelProvider", new XAttribute("value", ModelProvider)),
            new XElement("RequestType", new XAttribute("value", TemplateType)),
            new XElement("SQLPrompt", SQLPrompt.ToXml("Calculation")),
            new XElement("NaturalLanguagePrompt", NaturalLanguagePrompt.ToXml("Calculation")),
            new XElement("FindRequestPrompt", FindRequestPrompt.ToXml("Calculation")),
            new XElement("RAGPPrompt", RAGPrompt.ToXml("Calculation")),
            new XElement("Option", new XAttribute("state", Option ? "True" : "False")));

    public override string ToDisplayLine() =>
        "Configure Prompt Template [ " + "Template Name: " + TemplateName.Text + " ; " + "Model Provider: " + ModelProviderHr(ModelProvider) + " ; " + "Template Type: " + TemplateTypeHr(TemplateType) + " ; " + "SQL Prompt: " + SQLPrompt.Text + " ; " + "Natural Language Prompt: " + NaturalLanguagePrompt.Text + " ; " + "Find Request Prompt: " + FindRequestPrompt.Text + " ; " + "RAG Prompt: " + RAGPrompt.Text + " ; " + (Option ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var templateName_vWrapEl = step.Element("TemplateName");
        var templateName_vCalcEl = templateName_vWrapEl?.Element("Calculation");
        var templateName_v = templateName_vCalcEl is not null ? Calculation.FromXml(templateName_vCalcEl) : new Calculation("");
        var modelProvider_v = step.Element("ModelProvider")?.Attribute("value")?.Value ?? "OpenAI";
        var templateType_v = step.Element("RequestType")?.Attribute("value")?.Value ?? "SQL Query";
        var sQLPrompt_vWrapEl = step.Element("SQLPrompt");
        var sQLPrompt_vCalcEl = sQLPrompt_vWrapEl?.Element("Calculation");
        var sQLPrompt_v = sQLPrompt_vCalcEl is not null ? Calculation.FromXml(sQLPrompt_vCalcEl) : new Calculation("");
        var naturalLanguagePrompt_vWrapEl = step.Element("NaturalLanguagePrompt");
        var naturalLanguagePrompt_vCalcEl = naturalLanguagePrompt_vWrapEl?.Element("Calculation");
        var naturalLanguagePrompt_v = naturalLanguagePrompt_vCalcEl is not null ? Calculation.FromXml(naturalLanguagePrompt_vCalcEl) : new Calculation("");
        var findRequestPrompt_vWrapEl = step.Element("FindRequestPrompt");
        var findRequestPrompt_vCalcEl = findRequestPrompt_vWrapEl?.Element("Calculation");
        var findRequestPrompt_v = findRequestPrompt_vCalcEl is not null ? Calculation.FromXml(findRequestPrompt_vCalcEl) : new Calculation("");
        var rAGPrompt_vWrapEl = step.Element("RAGPPrompt");
        var rAGPrompt_vCalcEl = rAGPrompt_vWrapEl?.Element("Calculation");
        var rAGPrompt_v = rAGPrompt_vCalcEl is not null ? Calculation.FromXml(rAGPrompt_vCalcEl) : new Calculation("");
        var option_v = step.Element("Option")?.Attribute("state")?.Value == "True";
        return new ConfigurePromptTemplateStep(templateName_v, modelProvider_v, templateType_v, sQLPrompt_v, naturalLanguagePrompt_v, findRequestPrompt_v, rAGPrompt_v, option_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? templateName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Template Name:", StringComparison.OrdinalIgnoreCase)) { templateName_v = new Calculation(tok.Substring(14).Trim()); break; } }
        string modelProvider_v = "OpenAI";
        foreach (var tok in tokens) { if (tok.StartsWith("Model Provider:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(15).Trim(); modelProvider_v = ModelProviderXml(v); break; } }
        string templateType_v = "SQL Query";
        foreach (var tok in tokens) { if (tok.StartsWith("Template Type:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(14).Trim(); templateType_v = TemplateTypeXml(v); break; } }
        Calculation? sQLPrompt_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("SQL Prompt:", StringComparison.OrdinalIgnoreCase)) { sQLPrompt_v = new Calculation(tok.Substring(11).Trim()); break; } }
        Calculation? naturalLanguagePrompt_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Natural Language Prompt:", StringComparison.OrdinalIgnoreCase)) { naturalLanguagePrompt_v = new Calculation(tok.Substring(24).Trim()); break; } }
        Calculation? findRequestPrompt_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Find Request Prompt:", StringComparison.OrdinalIgnoreCase)) { findRequestPrompt_v = new Calculation(tok.Substring(20).Trim()); break; } }
        Calculation? rAGPrompt_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("RAG Prompt:", StringComparison.OrdinalIgnoreCase)) { rAGPrompt_v = new Calculation(tok.Substring(11).Trim()); break; } }
        bool option_v = false;
        return new ConfigurePromptTemplateStep(templateName_v, modelProvider_v, templateType_v, sQLPrompt_v, naturalLanguagePrompt_v, findRequestPrompt_v, rAGPrompt_v, option_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Template Name",
            },
            new ParamMetadata
            {
                Name = "ModelProvider",
                XmlElement = "ModelProvider",
                Type = "enum",
                HrLabel = "Model Provider",
                ValidValues = ["OpenAI", "Anthropic", "Cohere", "Custom"],
                DefaultValue = "OpenAI",
            },
            new ParamMetadata
            {
                Name = "RequestType",
                XmlElement = "RequestType",
                Type = "enum",
                HrLabel = "Template Type",
                ValidValues = ["SQL Query", "Find Request", "RAG Prompt"],
                DefaultValue = "SQL Query",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "SQL Prompt",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Natural Language Prompt",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Find Request Prompt",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "RAG Prompt",
            },
            new ParamMetadata
            {
                Name = "Option",
                XmlElement = "Option",
                Type = "boolean",
                XmlAttr = "state",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
