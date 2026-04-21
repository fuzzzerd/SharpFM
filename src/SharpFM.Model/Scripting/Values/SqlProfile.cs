using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// ODBC connection + query profile used by Execute SQL. The Query/QueryCalc
/// child is modeled as a union: either a literal <c>&lt;Query&gt;</c> text
/// element or a <c>&lt;QueryCalc&gt;</c> wrapping a Calculation. The
/// <c>QueryType</c> attribute indicates which is used.
/// </summary>
public sealed record SqlProfile(
    string QueryType,
    string Flags,
    string Password,
    string UserName,
    string Dsn,
    string FieldDelimiter,
    string IsPredefined,
    string FieldNameRow,
    string DataType,
    string? Query,
    Calculation? QueryCalc)
{
    public XElement ToXml()
    {
        var el = new XElement("Profile",
            new XAttribute("QueryType", QueryType),
            new XAttribute("flags", Flags),
            new XAttribute("password", Password),
            new XAttribute("UserName", UserName),
            new XAttribute("dsn", Dsn),
            new XAttribute("FieldDelimiter", FieldDelimiter),
            new XAttribute("IsPredefined", IsPredefined),
            new XAttribute("FieldNameRow", FieldNameRow),
            new XAttribute("DataType", DataType));
        if (Query is not null) el.Add(new XElement("Query", Query));
        if (QueryCalc is not null) el.Add(new XElement("QueryCalc", QueryCalc.ToXml("Calculation")));
        return el;
    }

    public static SqlProfile FromXml(XElement element)
    {
        var qEl = element.Element("Query");
        var qcEl = element.Element("QueryCalc")?.Element("Calculation");
        return new SqlProfile(
            element.Attribute("QueryType")?.Value ?? "Query",
            element.Attribute("flags")?.Value ?? "0",
            element.Attribute("password")?.Value ?? "",
            element.Attribute("UserName")?.Value ?? "",
            element.Attribute("dsn")?.Value ?? "",
            element.Attribute("FieldDelimiter")?.Value ?? "\t",
            element.Attribute("IsPredefined")?.Value ?? "-1",
            element.Attribute("FieldNameRow")?.Value ?? "0",
            element.Attribute("DataType")?.Value ?? "ODBC",
            qEl?.Value,
            qcEl is not null ? Calculation.FromXml(qcEl) : null);
    }
}
