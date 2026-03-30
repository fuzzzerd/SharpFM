// Step catalog data model — ported from agentic-fm (https://github.com/petrowsky/agentic-fm)
// Copyright 2026 Matt Petrowsky, Apache License 2.0

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpFM.Core.ScriptConverter;

public record StepDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("category")]
    public string Category { get; init; } = "";

    [JsonPropertyName("selfClosing")]
    public bool SelfClosing { get; init; }

    [JsonPropertyName("hrSignature")]
    public string? HrSignature { get; init; }

    [JsonPropertyName("params")]
    public StepParam[] Params { get; init; } = [];

    [JsonPropertyName("blockPair")]
    public StepBlockPair? BlockPair { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("helpUrl")]
    public string? HelpUrl { get; init; }

    [JsonPropertyName("snippetFile")]
    public string? SnippetFile { get; init; }

    [JsonPropertyName("monacoSnippet")]
    public string? MonacoSnippet { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public record StepParam
{
    [JsonPropertyName("xmlElement")]
    public string XmlElement { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("hrLabel")]
    public string? HrLabel { get; init; }

    [JsonPropertyName("xmlAttr")]
    public string? XmlAttr { get; init; }

    [JsonPropertyName("enumValues")]
    public JsonElement[]? EnumValues { get; init; }

    [JsonPropertyName("hrEnumValues")]
    public Dictionary<string, string?>? HrEnumValues { get; init; }

    [JsonPropertyName("hrValues")]
    public string[]? HrValues { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; init; }

    [JsonPropertyName("wrapperElement")]
    public string? WrapperElement { get; init; }

    [JsonPropertyName("parentElement")]
    public string? ParentElement { get; init; }

    [JsonPropertyName("invertedHr")]
    public bool InvertedHr { get; init; }

    [JsonPropertyName("flagStyle")]
    public bool FlagStyle { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public record StepBlockPair
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = "";

    [JsonPropertyName("partners")]
    public string[] Partners { get; init; } = [];
}
