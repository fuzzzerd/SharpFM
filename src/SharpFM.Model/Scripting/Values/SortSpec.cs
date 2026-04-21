using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// One field in a sort spec. <c>Type</c> is "Ascending", "Descending",
/// or "Custom" (the last requires a ValueList). Supports an optional
/// SummaryField (when sorting by a summary), ValueList (for Custom type),
/// and OverrideLanguage string.
/// </summary>
public sealed record SortField(
    string Type,
    FieldRef PrimaryField,
    FieldRef? SummaryField = null,
    NamedRef? ValueList = null,
    string? OverrideLanguage = null)
{
    public XElement ToXml()
    {
        var el = new XElement("Sort", new XAttribute("type", Type),
            new XElement("PrimaryField", PrimaryField.ToXml("Field")));
        if (SummaryField is not null)
            el.Add(new XElement("SummaryField", SummaryField.ToXml("Field")));
        if (ValueList is not null)
            el.Add(ValueList.ToXml("ValueList"));
        if (OverrideLanguage is not null)
            el.Add(new XElement("OverrideLanguage", new XAttribute("language", OverrideLanguage)));
        return el;
    }

    public static SortField FromXml(XElement element)
    {
        var primEl = element.Element("PrimaryField")?.Element("Field");
        var prim = primEl is not null ? FieldRef.FromXml(primEl) : FieldRef.ForField("", 0, "");
        var sumEl = element.Element("SummaryField")?.Element("Field");
        var sum = sumEl is not null ? FieldRef.FromXml(sumEl) : null;
        var vlEl = element.Element("ValueList");
        var vl = vlEl is not null ? NamedRef.FromXml(vlEl) : null;
        var lang = element.Element("OverrideLanguage")?.Attribute("language")?.Value;
        return new SortField(
            element.Attribute("type")?.Value ?? "Ascending",
            prim, sum, vl, lang);
    }
}

/// <summary>
/// A Sort Records sort order: Maintain flag controls whether changed
/// records stay in position until next sort, plus an ordered list of
/// <see cref="SortField"/>s.
/// </summary>
public sealed record SortList(bool Maintain, bool Value, IReadOnlyList<SortField> Fields)
{
    public XElement ToXml()
    {
        var el = new XElement("SortList",
            new XAttribute("Maintain", Maintain ? "True" : "False"),
            new XAttribute("value", Value ? "True" : "False"));
        foreach (var f in Fields) el.Add(f.ToXml());
        return el;
    }

    public static SortList FromXml(XElement element) =>
        new(
            element.Attribute("Maintain")?.Value == "True",
            element.Attribute("value")?.Value == "True",
            element.Elements("Sort").Select(SortField.FromXml).ToList());
}
