using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SharpFM.Model.Schema;

public class FmTable
{
    public string Name { get; set; } = "";
    public int? Id { get; set; }
    public List<FmField> Fields { get; }

    public FmTable(string name, List<FmField>? fields = null)
    {
        Name = name;
        Fields = fields ?? new List<FmField>();
    }

    public static FmTable FromXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return new FmTable("");

        XDocument doc = XDocument.Parse(xml);

        var root = doc.Root;
        if (root == null) return new FmTable("");

        var baseTable = root.Element("BaseTable") ?? root;
        var tableName = baseTable.Attribute("name")?.Value ?? "";
        var tableId = int.TryParse(baseTable.Attribute("id")?.Value, out var id) ? id : (int?)null;

        var fields = baseTable.Elements("Field")
            .Select(FmField.FromXml)
            .ToList();

        return new FmTable(tableName, fields) { Id = tableId };
    }

    public string ToXml()
    {
        var root = new XElement("fmxmlsnippet", new XAttribute("type", "FMObjectList"));
        var baseTable = new XElement("BaseTable", new XAttribute("name", Name));
        if (Id.HasValue)
            baseTable.Add(new XAttribute("id", Id.Value));

        foreach (var field in Fields)
            baseTable.Add(field.ToXml());

        root.Add(baseTable);

        return PrettyPrint(root.ToString());
    }

    public void AddField(FmField field)
    {
        Fields.Add(field);
    }

    public void RemoveField(FmField field)
    {
        Fields.Remove(field);
    }

    private static string PrettyPrint(string xml)
    {
        try
        {
            var element = XElement.Parse(xml);
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineOnAttributes = false
            };
            using (var writer = XmlWriter.Create(sb, settings))
            {
                element.Save(writer);
            }
            return sb.ToString();
        }
        catch
        {
            return xml;
        }
    }
}
