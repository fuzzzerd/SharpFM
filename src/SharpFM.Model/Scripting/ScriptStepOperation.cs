using System.Collections.Generic;

namespace SharpFM.Model.Scripting;

/// <summary>
/// A single script step mutation within a batch update.
/// </summary>
/// <param name="Action">"add", "update", "remove", or "move".</param>
/// <param name="Index">Target step index (for update/remove/move). For add, the insertion index (-1 = append).</param>
/// <param name="StepName">Step name from the catalog (for add only, e.g., "Set Variable", "If").</param>
/// <param name="Enabled">Whether the step is enabled (null = don't change).</param>
/// <param name="Params">Parameter values to set, keyed by param name (e.g., "Title", "Value"). Null = don't change.</param>
/// <param name="MoveToIndex">Destination index (for move only).</param>
public record ScriptStepOperation(
    string Action,
    int Index = -1,
    string? StepName = null,
    bool? Enabled = null,
    Dictionary<string, string?>? Params = null,
    int? MoveToIndex = null);
