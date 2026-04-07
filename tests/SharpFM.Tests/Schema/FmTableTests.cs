using System.Xml.Linq;
using SharpFM.Model.Schema;
using Xunit;

namespace SharpFM.Tests.Schema;

public class FmTableTests
{
    private static string Wrap(string inner) =>
        $"<fmxmlsnippet type=\"FMObjectList\">{inner}</fmxmlsnippet>";

    [Fact]
    public void FromXml_ParsesTableName()
    {
        var xml = Wrap("<BaseTable name=\"Invoices\" id=\"1\"></BaseTable>");
        var table = FmTable.FromXml(xml);
        Assert.Equal("Invoices", table.Name);
        Assert.Equal(1, table.Id);
    }

    [Fact]
    public void FromXml_ParsesAllFields()
    {
        var xml = Wrap(
            "<BaseTable name=\"T\">"
            + "<Field id=\"1\" name=\"A\" dataType=\"Text\" fieldType=\"Normal\"/>"
            + "<Field id=\"2\" name=\"B\" dataType=\"Number\" fieldType=\"Normal\"/>"
            + "</BaseTable>");
        var table = FmTable.FromXml(xml);
        Assert.Equal(2, table.Fields.Count);
    }

    [Fact]
    public void FromXml_EmptyTable()
    {
        var xml = Wrap("<BaseTable name=\"Empty\"></BaseTable>");
        var table = FmTable.FromXml(xml);
        Assert.Equal("Empty", table.Name);
        Assert.Empty(table.Fields);
    }

    [Fact]
    public void FromXml_MixedFieldTypes()
    {
        var xml = Wrap(
            "<BaseTable name=\"T\">"
            + "<Field id=\"1\" name=\"Name\" dataType=\"Text\" fieldType=\"Normal\"/>"
            + "<Field id=\"2\" name=\"Total\" dataType=\"Number\" fieldType=\"Calculated\">"
            + "<Calculation><![CDATA[A + B]]></Calculation></Field>"
            + "<Field id=\"3\" name=\"GrandTotal\" dataType=\"Number\" fieldType=\"Summary\">"
            + "<SummaryField operation=\"Sum\"><SummaryField name=\"Total\"/></SummaryField></Field>"
            + "</BaseTable>");
        var table = FmTable.FromXml(xml);
        Assert.Equal(3, table.Fields.Count);
        Assert.Equal(FieldKind.Normal, table.Fields[0].Kind);
        Assert.Equal(FieldKind.Calculated, table.Fields[1].Kind);
        Assert.Equal(FieldKind.Summary, table.Fields[2].Kind);
    }

    [Fact]
    public void ToXml_OutputIsValid()
    {
        var table = new FmTable("Test");
        table.AddField(new FmField { Id = 1, Name = "F1", DataType = FieldDataType.Text });
        var xml = table.ToXml();
        XDocument.Parse(xml); // should not throw
    }

    [Fact]
    public void ToXml_PreservesTableName()
    {
        var table = new FmTable("Contacts") { Id = 5 };
        var xml = table.ToXml();
        var doc = XDocument.Parse(xml);
        var bt = doc.Root!.Element("BaseTable");
        Assert.Equal("Contacts", bt?.Attribute("name")?.Value);
        Assert.Equal("5", bt?.Attribute("id")?.Value);
    }

    [Fact]
    public void AddField_IncreasesCount()
    {
        var table = new FmTable("T");
        Assert.Empty(table.Fields);
        table.AddField(new FmField { Name = "New" });
        Assert.Single(table.Fields);
    }

    [Fact]
    public void RemoveField_DecreasesCount()
    {
        var field = new FmField { Name = "ToRemove" };
        var table = new FmTable("T", new() { field });
        Assert.Single(table.Fields);
        table.RemoveField(field);
        Assert.Empty(table.Fields);
    }
}
