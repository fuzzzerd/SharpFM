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
    /// Canonical display form: <c>Table::Field</c>, bare <c>Field</c> when
    /// table is absent, or <c>$variable</c> for variables.
    /// </summary>
    public string ToDisplayString()
    {
        if (IsVariable) return VariableName!;
        if (!string.IsNullOrEmpty(Table)) return $"{Table}::{Name}";
        return Name;
    }
}
