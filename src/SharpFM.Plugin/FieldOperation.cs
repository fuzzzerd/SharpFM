// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

namespace SharpFM.Plugin;

/// <summary>
/// A single field mutation within a batch update to a table clip.
/// </summary>
/// <param name="Action">"add", "modify", or "remove".</param>
/// <param name="FieldName">Target field name (for modify/remove) or new field name (for add).</param>
/// <param name="NewName">New name when renaming (modify only).</param>
/// <param name="DataType">"Text", "Number", "Date", "Time", "TimeStamp", or "Binary".</param>
/// <param name="Kind">"Normal", "Calculated", or "Summary".</param>
/// <param name="Comment">Field comment.</param>
/// <param name="Calculation">Calculation expression (for Calculated fields).</param>
/// <param name="IsGlobal">Whether the field uses global storage.</param>
/// <param name="Repetitions">Number of repetitions (default 1).</param>
public record FieldOperation(
    string Action,
    string FieldName,
    string? NewName = null,
    string? DataType = null,
    string? Kind = null,
    string? Comment = null,
    string? Calculation = null,
    bool? IsGlobal = null,
    int? Repetitions = null);
