using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ConfigurePromptTemplateStep : ScriptStep<ConfigurePromptTemplateStep>, IStepFactory
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

    // Hand-written: trailing bare On/Off token and token order diverging from
    // the canonical XML order.
    public override string ToDisplayLine() =>
        "Configure Prompt Template [ " + "Template Name: " + TemplateName.Text + " ; " + "Model Provider: " + ModelProviderHr(ModelProvider) + " ; " + "Template Type: " + TemplateTypeHr(TemplateType) + " ; " + "SQL Prompt: " + SQLPrompt.Text + " ; " + "Natural Language Prompt: " + NaturalLanguagePrompt.Text + " ; " + "Find Request Prompt: " + FindRequestPrompt.Text + " ; " + "RAG Prompt: " + RAGPrompt.Text + " ; " + (Option ? "On" : "Off") + " ]";

    protected internal override void PopulateFromDisplay(string[] hrParams)
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
        TemplateName = templateName_v ?? new Calculation("");
        ModelProvider = modelProvider_v;
        TemplateType = templateType_v;
        SQLPrompt = sQLPrompt_v ?? new Calculation("");
        NaturalLanguagePrompt = naturalLanguagePrompt_v ?? new Calculation("");
        FindRequestPrompt = findRequestPrompt_v ?? new Calculation("");
        RAGPrompt = rAGPrompt_v ?? new Calculation("");
        Option = option_v;
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
                new NamedTextChild("ModelProvider") { PocoProperty = "ModelProvider", HrLabel = "Model Provider", DisplayValues = ["OpenAI", "Anthropic", "Cohere", "Custom"], Display = DisplayMode.Augmented },
                new NamedTextChild("RequestType") { PocoProperty = "TemplateType", HrLabel = "Template Type", DisplayValues = ["SQL Query", "Find Request", "RAG Prompt"], Display = DisplayMode.Augmented },
            ]),
            // The name and per-mode prompt calcs have no wire children in the
            // canonical form; HR-only slots keep their display tokens addressable.
            new HrOnly("TemplateName") { HrLabel = "Template Name" },
            new HrOnly("SQLPrompt") { HrLabel = "SQL Prompt" },
            new HrOnly("NaturalLanguagePrompt") { HrLabel = "Natural Language Prompt" },
            new HrOnly("FindRequestPrompt") { HrLabel = "Find Request Prompt" },
            new HrOnly("RAGPrompt") { HrLabel = "RAG Prompt" },
        ],
    };
}
