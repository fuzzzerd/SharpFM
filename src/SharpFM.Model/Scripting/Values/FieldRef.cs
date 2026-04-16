using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// A reference to a field, either by table+name (the usual <c>&lt;Field
/// table="People" id="1" name="FirstName"/&gt;</c> form) or a variable
/// reference for step-local variables. Used by Set Field, Set Variable,
/// Show Custom Dialog input fields, and anywhere the catalog declares a
/// "field" or "fieldOrVariable" param type.
///
/// The distinction between table-qualified and variable refs is carried by
/// <see cref="VariableName"/> being non-null — variables have no table/id
/// and render as <c>$name</c> in display text.
/// </summary>
public sealed record FieldRef
{
    /// <summary>Table occurrence name, or null for a variable reference.</summary>
    public string? Table { get; init; }

    /// <summary>Field id; preserved for round-trip. Zero for unresolved refs.</summary>
    public int Id { get; init; }

    /// <summary>Field name, or empty string for a variable reference.</summary>
    public string Name { get; init; } = "";

    /// <summary>Non-null when this reference is a variable (e.g. <c>$count</c>).</summary>
    public string? VariableName { get; init; }

    public bool IsVariable => VariableName is not null;

    public static FieldRef ForField(string? table, int id, string name) =>
        new() { Table = table, Id = id, Name = name };

    public static FieldRef ForVariable(string variableName) =>
        new() { VariableName = variableName };

    public XElement ToXml(string elementName)
    {
        if (IsVariable)
        {
            return new XElement(elementName, VariableName);
        }

        return new XElement(elementName,
            new XAttribute("table", Table ?? ""),
            new XAttribute("id", Id),
            new XAttribute("name", Name));
    }

    public static FieldRef FromXml(XElement element)
    {
        var table = element.Attribute("table")?.Value;
        var name = element.Attribute("name")?.Value;

        if (!string.IsNullOrEmpty(name))
        {
            var idStr = element.Attribute("id")?.Value;
            var id = int.TryParse(idStr, out var parsed) ? parsed : 0;
            return ForField(string.IsNullOrEmpty(table) ? null : table, id, name);
        }

        // No name attribute — treat element text as a variable reference.
        var text = element.Value;
        return ForVariable(text);
    }

    /// <summary>
    /// Canonical lossless display form: <c>Table::Field (#id)</c> when the id
    /// is known (non-zero), <c>Table::Field</c> otherwise. Variables render as
    /// <c>$var</c> with no id. Matches the <c>(#id)</c> convention used by
    /// GoToLayoutStep / PerformScriptStep for named refs.
    /// </summary>
    public string ToDisplayString() => ToDisplayString(includeId: true);

    /// <summary>
    /// Display form with optional <c>(#id)</c> suffix. Callers that need to
    /// compose the field reference with an additional suffix (e.g. a
    /// <c>[rep]</c> repetition marker that must sit between the name and
    /// the id annotation) should use <c>includeId: false</c> and append the
    /// id themselves.
    /// </summary>
    public string ToDisplayString(bool includeId)
    {
        if (IsVariable) return VariableName!;
        var baseText = !string.IsNullOrEmpty(Table) ? $"{Table}::{Name}" : Name;
        return includeId && Id > 0 ? $"{baseText} (#{Id})" : baseText;
    }

    /// <summary>
    /// Parse a display-text token back into a <see cref="FieldRef"/>. Accepts:
    /// <list type="bullet">
    ///   <item><c>Table::Name (#id)</c> — table-qualified field with id</item>
    ///   <item><c>Table::Name</c> — table-qualified field, id=0 (unresolved)</item>
    ///   <item><c>Name (#id)</c> — bare field with id</item>
    ///   <item><c>Name</c> — bare field, id=0</item>
    ///   <item><c>$var</c> or <c>$$var</c> — variable</item>
    /// </list>
    /// Future work will allow threading an <c>INameResolver</c> to fill id=0
    /// values from a loaded DDL dictionary; the signature accepts a trailing
    /// optional parameter so call sites don't need to change.
    /// </summary>
    public static FieldRef FromDisplayToken(string token)
    {
        var t = token.Trim();

        if (t.StartsWith("$"))
            return ForVariable(t);

        var idMatch = IdSuffix.Match(t);
        int id = 0;
        if (idMatch.Success)
        {
            id = int.Parse(idMatch.Groups["id"].Value);
            t = t.Substring(0, idMatch.Index).TrimEnd();
        }

        var sep = t.IndexOf("::");
        if (sep >= 0)
        {
            var table = t.Substring(0, sep);
            var name = t.Substring(sep + 2);
            return ForField(string.IsNullOrEmpty(table) ? null : table, id, name);
        }

        return ForField(null, id, t);
    }

    private static readonly Regex IdSuffix = new(
        @"\s*\(#(?<id>\d+)\)\s*$",
        RegexOptions.Compiled);
}
