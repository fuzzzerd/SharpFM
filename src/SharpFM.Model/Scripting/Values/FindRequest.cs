using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// One clause within a find request — a field reference and a literal
/// query text. The text uses FM's find operators (<c>=</c>, <c>&gt;</c>,
/// range syntax, etc.) but is stored as a raw string; we don't parse it.
/// </summary>
public sealed record FindCriterion(FieldRef Field, string Query)
{
    public XElement ToXml() =>
        new("Criteria",
            Field.ToXml("Field"),
            new XElement("Text", Query));

    public static FindCriterion FromXml(XElement element)
    {
        var fieldEl = element.Element("Field");
        var field = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, "");
        var query = element.Element("Text")?.Value ?? "";
        return new FindCriterion(field, query);
    }
}

/// <summary>
/// One find request — either "Include" (find matching) or "Exclude"
/// (omit matching) — containing one or more criteria. Multiple
/// <see cref="FindRequest"/>s in a <see cref="FindRequestList"/> act
/// as OR across requests; criteria within one request AND together.
/// </summary>
public sealed record FindRequest(string Operation, IReadOnlyList<FindCriterion> Criteria)
{
    public XElement ToXml()
    {
        var row = new XElement("RequestRow", new XAttribute("operation", Operation));
        foreach (var c in Criteria) row.Add(c.ToXml());
        return row;
    }

    public static FindRequest FromXml(XElement element) =>
        new(
            element.Attribute("operation")?.Value ?? "Include",
            element.Elements("Criteria").Select(FindCriterion.FromXml).ToList());
}

/// <summary>
/// Shared <c>&lt;Query&gt;</c> wrapper used by Constrain Found Set,
/// Extend Found Set, Enter Find Mode, and Perform Find.
/// </summary>
public sealed record FindRequestList(IReadOnlyList<FindRequest> Requests)
{
    public XElement ToXml()
    {
        var q = new XElement("Query");
        foreach (var r in Requests) q.Add(r.ToXml());
        return q;
    }

    public static FindRequestList FromXml(XElement element) =>
        new(element.Elements("RequestRow").Select(FindRequest.FromXml).ToList());

    public static FindRequestList Empty() => new(new List<FindRequest>());
}
