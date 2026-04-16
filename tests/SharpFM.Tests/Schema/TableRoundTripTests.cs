using System.Xml.Linq;
using SharpFM.Model.Schema;
using Xunit;

namespace SharpFM.Tests.Schema;

public class TableRoundTripTests
{
    private static string Wrap(string inner) =>
        $"<fmxmlsnippet type=\"FMObjectList\">{inner}</fmxmlsnippet>";

    [Fact]
    public void NormalFields_RoundTrip()
    {
        var xml = Wrap(
            "<BaseTable name=\"People\">"
            + "<Field id=\"1\" name=\"FirstName\" dataType=\"Text\" fieldType=\"Normal\"/>"
            + "<Field id=\"2\" name=\"Age\" dataType=\"Number\" fieldType=\"Normal\"/>"
            + "</BaseTable>");
        var table = FmTable.FromXml(xml);
        var output = table.ToXml();
        var table2 = FmTable.FromXml(output);

        Assert.Equal(table.Name, table2.Name);
        Assert.Equal(table.Fields.Count, table2.Fields.Count);
        Assert.Equal("FirstName", table2.Fields[0].Name);
        Assert.Equal(FieldDataType.Number, table2.Fields[1].DataType);
    }

    [Fact]
    public void CalculatedField_RoundTrip()
    {
        var xml = Wrap(
            "<BaseTable name=\"T\">"
            + "<Field id=\"1\" name=\"Total\" dataType=\"Number\" fieldType=\"Calculated\">"
            + "<Calculation alwaysEvaluate=\"True\"><![CDATA[Qty * Price]]></Calculation></Field>"
            + "</BaseTable>");
        var table = FmTable.FromXml(xml);
        var table2 = FmTable.FromXml(table.ToXml());

        Assert.Equal("Qty * Price", table2.Fields[0].Calculation);
        Assert.True(table2.Fields[0].AlwaysEvaluate);
    }

    [Fact]
    public void SummaryField_RoundTrip()
    {
        var xml = Wrap(
            "<BaseTable name=\"T\">"
            + "<Field id=\"1\" name=\"Sum\" dataType=\"Number\" fieldType=\"Summary\">"
            + "<SummaryField operation=\"Average\">"
            + "<SummaryField name=\"T::Score\"/>"
            + "</SummaryField></Field>"
            + "</BaseTable>");
        var table = FmTable.FromXml(xml);
        var table2 = FmTable.FromXml(table.ToXml());

        Assert.Equal(SummaryOperation.Average, table2.Fields[0].SummaryOp);
        Assert.Equal("T::Score", table2.Fields[0].SummaryTargetField);
    }

    [Fact]
    public void AutoEnterFields_RoundTrip()
    {
        var xml = Wrap(
            "<BaseTable name=\"T\">"
            + "<Field id=\"1\" name=\"ID\" dataType=\"Number\" fieldType=\"Normal\">"
            + "<AutoEnter value=\"SerialNumber\" allowEditing=\"True\">"
            + "<Serial nextSerialNumber=\"500\"/>"
            + "</AutoEnter></Field>"
            + "</BaseTable>");
        var table = FmTable.FromXml(xml);
        var table2 = FmTable.FromXml(table.ToXml());

        Assert.Equal(AutoEnterType.Serial, table2.Fields[0].AutoEnter);
        Assert.True(table2.Fields[0].AllowEditing);
        Assert.Equal("500", table2.Fields[0].AutoEnterValue);
    }

    [Fact]
    public void ValidationRules_RoundTrip()
    {
        var xml = Wrap(
            "<BaseTable name=\"T\">"
            + "<Field id=\"1\" name=\"Email\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Validation>"
            + "<NotEmpty value=\"True\"/>"
            + "<Unique value=\"True\"/>"
            + "<MaxDataLength length=\"200\"/>"
            + "<Range><MinimumValue>1</MinimumValue><MaximumValue>100</MaximumValue></Range>"
            + "<ErrorMessage><![CDATA[Invalid]]></ErrorMessage>"
            + "</Validation></Field>"
            + "</BaseTable>");
        var table = FmTable.FromXml(xml);
        var table2 = FmTable.FromXml(table.ToXml());

        var f = table2.Fields[0];
        Assert.True(f.NotEmpty);
        Assert.True(f.Unique);
        Assert.Equal("200", f.MaxDataLength);
        Assert.Equal("1", f.RangeMin);
        Assert.Equal("100", f.RangeMax);
        Assert.Equal("Invalid", f.ErrorMessage);
    }

    [Fact]
    public void StorageOptions_RoundTrip()
    {
        var xml = Wrap(
            "<BaseTable name=\"T\">"
            + "<Field id=\"1\" name=\"G\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Storage global=\"True\" indexed=\"Minimal\"/></Field>"
            + "</BaseTable>");
        var table = FmTable.FromXml(xml);
        var table2 = FmTable.FromXml(table.ToXml());

        Assert.True(table2.Fields[0].IsGlobal);
        Assert.Equal(FieldIndexing.Minimal, table2.Fields[0].Indexing);
    }

    [Fact]
    public void MixedTable_RoundTrip()
    {
        var xml = Wrap(
            "<BaseTable name=\"Orders\" id=\"42\">"
            + "<Field id=\"1\" name=\"OrderID\" dataType=\"Number\" fieldType=\"Normal\">"
            + "<AutoEnter value=\"SerialNumber\"><Serial nextSerialNumber=\"1\"/></AutoEnter>"
            + "<Validation><NotEmpty value=\"True\"/><Unique value=\"True\"/></Validation></Field>"
            + "<Field id=\"2\" name=\"Customer\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Validation><NotEmpty value=\"True\"/></Validation></Field>"
            + "<Field id=\"3\" name=\"OrderDate\" dataType=\"Date\" fieldType=\"Normal\">"
            + "<AutoEnter value=\"CreationDate\"/></Field>"
            + "<Field id=\"4\" name=\"Total\" dataType=\"Number\" fieldType=\"Calculated\">"
            + "<Calculation><![CDATA[Sum(LineItems::Amount)]]></Calculation></Field>"
            + "<Field id=\"5\" name=\"GrandTotal\" dataType=\"Number\" fieldType=\"Summary\">"
            + "<SummaryField operation=\"Sum\"><SummaryField name=\"Total\"/></SummaryField></Field>"
            + "<Field id=\"6\" name=\"Notes\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Comment>Internal notes</Comment>"
            + "<Storage global=\"False\" indexed=\"All\"/></Field>"
            + "</BaseTable>");

        var table = FmTable.FromXml(xml);
        Assert.Equal(6, table.Fields.Count);
        Assert.Equal("Orders", table.Name);
        Assert.Equal(42, table.Id);

        var table2 = FmTable.FromXml(table.ToXml());
        Assert.Equal(table.Fields.Count, table2.Fields.Count);
        Assert.Equal("Orders", table2.Name);
        Assert.Equal(42, table2.Id);

        // Spot check individual fields survived
        Assert.Equal(AutoEnterType.Serial, table2.Fields[0].AutoEnter);
        Assert.True(table2.Fields[0].NotEmpty);
        Assert.Equal(FieldKind.Calculated, table2.Fields[3].Kind);
        Assert.Equal("Sum(LineItems::Amount)", table2.Fields[3].Calculation);
        Assert.Equal(SummaryOperation.Sum, table2.Fields[4].SummaryOp);
        Assert.Equal("Internal notes", table2.Fields[5].Comment);
    }
}
