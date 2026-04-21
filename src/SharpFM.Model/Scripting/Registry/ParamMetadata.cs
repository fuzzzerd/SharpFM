using System.Collections.Generic;

namespace SharpFM.Model.Scripting.Registry;

/// <summary>
/// Typed description of a single script-step parameter, owned by the
/// parent POCO's <see cref="StepMetadata"/>. Intentionally leaner than
/// the legacy <c>StepParam</c> — fields that only catalog-driven helpers
/// consumed (<c>flagStyle</c>, <c>hrValues</c>, <c>hrEnumValues</c>,
/// <c>wrapperElement</c>, <c>parentElement</c>, <c>invertedHr</c>,
/// <c>monacoSnippet</c>, <c>snippetFile</c>, <c>status</c>) are dropped.
/// Add them back per-POCO if a concrete step needs them.
/// </summary>
public sealed record ParamMetadata
{
    /// <summary>Short canonical identifier for the parameter.</summary>
    public required string Name { get; init; }

    /// <summary>XML element name emitted for this parameter.</summary>
    public required string XmlElement { get; init; }

    /// <summary>
    /// Parameter type classifier — e.g. "boolean", "enum",
    /// "calculation", "text". Informs display formatting and
    /// intellisense behaviour.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Optional XML attribute name (e.g. "state", "value") when the
    /// parameter's value lives on an attribute rather than element text.
    /// </summary>
    public string? XmlAttr { get; init; }

    /// <summary>Human-readable label shown in display text and completion.</summary>
    public string? HrLabel { get; init; }

    /// <summary>Tooltip text for hover UIs, drawn from upstream snippet comments.</summary>
    public string? Description { get; init; }

    /// <summary>Closed set of permissible values for enum / boolean params.</summary>
    public IReadOnlyList<string>? ValidValues { get; init; }

    /// <summary>Default value emitted when the parameter is omitted.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>True when the parameter must be present in valid XML.</summary>
    public bool Required { get; init; }
}
