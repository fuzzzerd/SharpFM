// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

using System.Collections.Generic;

namespace SharpFM.Plugin;

/// <summary>
/// Read-only snapshot of a script step for plugin consumption.
/// </summary>
public record ScriptStepInfo(
    int Index,
    string StepName,
    bool Enabled,
    IReadOnlyList<ScriptStepParam> Params);

/// <summary>
/// A single parameter of a script step.
/// The Name is the human-readable label or wrapper element name (e.g., "Title", "Value", "Table").
/// </summary>
public record ScriptStepParam(string Name, string Type, string? Value);
