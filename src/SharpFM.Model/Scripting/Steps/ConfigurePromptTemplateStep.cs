using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ConfigurePromptTemplateStep : ScriptStep, IStepFactory
{
    public const int XmlId = 226;
    public const string XmlName = "Configure Prompt Template";

    public Calculation TemplateName { get; set; } = new("");
    public string ModelProvider { get; set; } = "OpenAI";
    public string TemplateType { get; set; } = "SQL Query";
    public Calculation SQLPrompt { get; set; } = new("");
    public Calculation NaturalLanguagePrompt { get; set; } = new("");
    public Calculation FindRequestPrompt { get; set; } = new("");
    public Calculation RAGPrompt { get; set; } = new("");
    public bool Option { get; set; }

    private ConfigurePromptTemplateStep() : base(false) { }

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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Configure Prompt Template [ " + "Template Name: " + TemplateName.Text + " ; " + "Model Provider: " + ModelProviderHr(ModelProvider) + " ; " + "Template Type: " + TemplateTypeHr(TemplateType) + " ; " + "SQL Prompt: " + SQLPrompt.Text + " ; " + "Natural Language Prompt: " + NaturalLanguagePrompt.Text + " ; " + "Find Request Prompt: " + FindRequestPrompt.Text + " ; " + "RAG Prompt: " + RAGPrompt.Text + " ; " + (Option ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ConfigurePromptTemplateStep>(step, Metadata);

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
        // Canonical: Option, then a <ConfigurePromptTemplate> wrapper carrying
        // the text <ModelProvider> and <RequestType> children.
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "Option", Display = DisplayMode.Native },
            new WrapperChild("ConfigurePromptTemplate",
            [
                new NamedTextChild("ModelProvider") { PocoProperty = "ModelProvider", HrLabel = "Model Provider", Display = DisplayMode.Augmented },
                new NamedTextChild("RequestType") { PocoProperty = "TemplateType", HrLabel = "Template Type", Display = DisplayMode.Augmented },
            ]),
        ],
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
