using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;

namespace SharpFM.Model.Schema;

public class FmTable
{
    public string Name { get; set; } = "";
    public int? Id { get; set; }
    public ObservableCollection<FmField> Fields { get; }

    public FmTable(string name, IEnumerable<FmField>? fields = null)
    {
        Name = name;
        Fields = fields is null ? new ObservableCollection<FmField>() : new ObservableCollection<FmField>(fields);
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
            .Select(FmField.FromXml);

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

        return XmlHelpers.PrettyPrint(root.ToString());
    }

    public void AddField(FmField field)
    {
        Fields.Add(field);
    }

    public void RemoveField(FmField field)
    {
        Fields.Remove(field);
    }

    // --- Apply mutation operations ---

    /// <summary>
    /// Apply a single field operation to this table. Returns any errors as a list
    /// (empty list = success).
    /// </summary>
    public IReadOnlyList<string> Apply(FieldOperation op) =>
        op.Action.ToLowerInvariant() switch
        {
            "add" => ApplyAdd(op),
            "modify" => ApplyModify(op),
            "remove" => ApplyRemove(op),
            _ => [$"Unknown action '{op.Action}' for field '{op.FieldName}'."],
        };

    private List<string> ApplyAdd(FieldOperation op)
    {
        if (Fields.Any(f => f.Name.Equals(op.FieldName, StringComparison.OrdinalIgnoreCase)))
            return [$"Field '{op.FieldName}' already exists."];

        var field = new FmField { Name = op.FieldName };
        ApplyFieldProperties(field, op);
        AddField(field);
        return [];
    }

    private List<string> ApplyModify(FieldOperation op)
    {
        var field = Fields.FirstOrDefault(f => f.Name.Equals(op.FieldName, StringComparison.OrdinalIgnoreCase));
        if (field is null) return [$"Field '{op.FieldName}' not found."];

        if (op.NewName is not null) field.Name = op.NewName;
        ApplyFieldProperties(field, op);
        return [];
    }

    private List<string> ApplyRemove(FieldOperation op)
    {
        var field = Fields.FirstOrDefault(f => f.Name.Equals(op.FieldName, StringComparison.OrdinalIgnoreCase));
        if (field is null) return [$"Field '{op.FieldName}' not found."];
        RemoveField(field);
        return [];
    }

    private static void ApplyFieldProperties(FmField field, FieldOperation op)
    {
        if (op.DataType is not null && Enum.TryParse<FieldDataType>(op.DataType, ignoreCase: true, out var dt)) field.DataType = dt;
        if (op.Kind is not null && Enum.TryParse<FieldKind>(op.Kind, ignoreCase: true, out var kind)) field.Kind = kind;
        if (op.Comment is not null) field.Comment = op.Comment;
        if (op.Calculation is not null) field.Calculation = op.Calculation;
        if (op.IsGlobal is not null) field.IsGlobal = op.IsGlobal.Value;
        if (op.Repetitions is not null) field.Repetitions = op.Repetitions.Value;
    }
}
