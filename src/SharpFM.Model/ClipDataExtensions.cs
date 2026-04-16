using System.Collections.Generic;
using SharpFM.Model.Schema;
using SharpFM.Model.Scripting;

namespace SharpFM.Model;

/// <summary>
/// Convenience extensions on <see cref="ClipData"/> for parsing into the
/// appropriate domain model based on the clip's format.
/// </summary>
public static class ClipDataExtensions
{
    public static bool IsScript(this ClipData clip) =>
        clip.ClipType is "Mac-XMSS" or "Mac-XMSC";

    public static bool IsTable(this ClipData clip) =>
        clip.ClipType is "Mac-XMTB" or "Mac-XMFD";

    /// <summary>
    /// Parse this clip as a script. Returns null if the clip is not a script type.
    /// </summary>
    public static FmScript? AsScript(this ClipData clip) =>
        clip.IsScript() ? FmScript.FromXml(clip.Xml) : null;

    /// <summary>
    /// Parse this clip as a table. Returns null if the clip is not a table type.
    /// </summary>
    public static FmTable? AsTable(this ClipData clip) =>
        clip.IsTable() ? FmTable.FromXml(clip.Xml) : null;

    /// <summary>
    /// Get the script's steps as a snapshot list. Returns null if the clip is not a script type.
    /// </summary>
    public static IReadOnlyList<ScriptStep>? GetScriptSteps(this ClipData clip) =>
        clip.AsScript()?.Steps;

    /// <summary>
    /// Get the table's fields as a snapshot list. Returns null if the clip is not a table type.
    /// </summary>
    public static IReadOnlyList<FmField>? GetTableFields(this ClipData clip) =>
        clip.AsTable()?.Fields;
}
