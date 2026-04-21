using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// One target-field entry in an Import Records field map. The
/// <c>map</c> attribute controls what happens to the field during
/// import: "Import", "DoNotImport", or "Match" (for UpdateOnMatch
/// key fields). FieldOptions is an opaque numeric bitmask preserved
/// as a string.
/// </summary>
public sealed record ImportTargetField(string FieldOptions, string Map, string Id, string Name, string? Table = null)
{
    public XElement ToXml()
    {
        var el = new XElement("Field",
            new XAttribute("FieldOptions", FieldOptions),
            new XAttribute("map", Map));
        if (Table is not null) el.Add(new XAttribute("table", Table));
        el.Add(new XAttribute("id", Id));
        el.Add(new XAttribute("name", Name));
        return el;
    }

    public static ImportTargetField FromXml(XElement element) =>
        new(
            element.Attribute("FieldOptions")?.Value ?? "0",
            element.Attribute("map")?.Value ?? "Import",
            element.Attribute("id")?.Value ?? "",
            element.Attribute("name")?.Value ?? "",
            element.Attribute("table")?.Value);
}

/// <summary>One entry in an Export Records field list.</summary>
public sealed record ExportEntry(FieldRef Field)
{
    public XElement ToXml() => new("ExportEntry", Field.ToXml("Field"));

    public static ExportEntry FromXml(XElement element)
    {
        var fieldEl = element.Element("Field");
        return new ExportEntry(fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, ""));
    }
}

/// <summary>
/// Entry in the Export Records SummaryFields list. Carries an optional
/// GroupByFieldIsSelected attribute on the underlying Field element.
/// </summary>
public sealed record SummaryFieldEntry(FieldRef Field, bool GroupByFieldIsSelected)
{
    public XElement ToXml()
    {
        // Build fresh with GroupByFieldIsSelected first to match FM Pro's attribute order.
        if (Field.IsVariable)
            return new XElement("Field",
                new XAttribute("GroupByFieldIsSelected", GroupByFieldIsSelected ? "True" : "False"),
                Field.VariableName);
        return new XElement("Field",
            new XAttribute("GroupByFieldIsSelected", GroupByFieldIsSelected ? "True" : "False"),
            new XAttribute("table", Field.Table ?? ""),
            new XAttribute("id", Field.Id),
            new XAttribute("name", Field.Name));
    }

    public static SummaryFieldEntry FromXml(XElement element)
    {
        var flag = element.Attribute("GroupByFieldIsSelected")?.Value == "True";
        return new SummaryFieldEntry(FieldRef.FromXml(element), flag);
    }
}
