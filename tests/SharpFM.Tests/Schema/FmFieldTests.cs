using System.Xml.Linq;
using SharpFM.Schema.Model;
using Xunit;

namespace SharpFM.Tests.Schema;

public class FmFieldTests
{
    private static FmField Parse(string xml) => FmField.FromXml(XElement.Parse(xml));

    [Fact]
    public void FromXml_NormalTextField()
    {
        var f = Parse("<Field id=\"1\" name=\"FirstName\" dataType=\"Text\" fieldType=\"Normal\"/>");
        Assert.Equal("FirstName", f.Name);
        Assert.Equal(FieldDataType.Text, f.DataType);
        Assert.Equal(FieldKind.Normal, f.Kind);
        Assert.Equal(1, f.Id);
    }

    [Fact]
    public void FromXml_NumberField()
    {
        var f = Parse("<Field id=\"2\" name=\"Amount\" dataType=\"Number\" fieldType=\"Normal\"/>");
        Assert.Equal(FieldDataType.Number, f.DataType);
    }

    [Fact]
    public void FromXml_DateField()
    {
        var f = Parse("<Field id=\"3\" name=\"DueDate\" dataType=\"Date\" fieldType=\"Normal\"/>");
        Assert.Equal(FieldDataType.Date, f.DataType);
    }

    [Fact]
    public void FromXml_CalculatedField_HasCalculation()
    {
        var f = Parse(
            "<Field id=\"4\" name=\"Total\" dataType=\"Number\" fieldType=\"Calculated\">"
            + "<Calculation><![CDATA[Qty * Price]]></Calculation></Field>");
        Assert.Equal(FieldKind.Calculated, f.Kind);
        Assert.Equal("Qty * Price", f.Calculation);
    }

    [Fact]
    public void FromXml_CalculatedField_AlwaysEvaluate()
    {
        var f = Parse(
            "<Field id=\"4\" name=\"Total\" dataType=\"Number\" fieldType=\"Calculated\">"
            + "<Calculation alwaysEvaluate=\"True\" table=\"Orders\"><![CDATA[Sum(Items::Price)]]></Calculation></Field>");
        Assert.True(f.AlwaysEvaluate);
        Assert.Equal("Orders", f.CalculationContext);
    }

    [Fact]
    public void FromXml_SummaryField_HasOperation()
    {
        var f = Parse(
            "<Field id=\"5\" name=\"TotalSales\" dataType=\"Number\" fieldType=\"Summary\">"
            + "<SummaryField operation=\"Sum\">"
            + "<SummaryField name=\"Items::Price\"/>"
            + "</SummaryField></Field>");
        Assert.Equal(FieldKind.Summary, f.Kind);
        Assert.Equal(SummaryOperation.Sum, f.SummaryOp);
        Assert.Equal("Items::Price", f.SummaryTargetField);
    }

    [Fact]
    public void FromXml_AutoEnterSerial()
    {
        var f = Parse(
            "<Field id=\"6\" name=\"ID\" dataType=\"Number\" fieldType=\"Normal\">"
            + "<AutoEnter value=\"SerialNumber\" allowEditing=\"False\">"
            + "<Serial nextSerialNumber=\"1001\"/>"
            + "</AutoEnter></Field>");
        Assert.Equal(AutoEnterType.Serial, f.AutoEnter);
        Assert.False(f.AllowEditing);
        Assert.Equal("1001", f.AutoEnterValue);
    }

    [Fact]
    public void FromXml_AutoEnterUUID()
    {
        var f = Parse(
            "<Field id=\"7\" name=\"UUID\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<AutoEnter value=\"UUID\"/></Field>");
        Assert.Equal(AutoEnterType.UUID, f.AutoEnter);
    }

    [Fact]
    public void FromXml_AutoEnterCalculation()
    {
        var f = Parse(
            "<Field id=\"8\" name=\"FullName\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<AutoEnter value=\"Calculation\">"
            + "<Calculation><![CDATA[First & \" \" & Last]]></Calculation>"
            + "</AutoEnter></Field>");
        Assert.Equal(AutoEnterType.Calculation, f.AutoEnter);
        Assert.Equal("First & \" \" & Last", f.AutoEnterValue);
    }

    [Fact]
    public void FromXml_AutoEnterCreationDate()
    {
        var f = Parse(
            "<Field id=\"9\" name=\"Created\" dataType=\"TimeStamp\" fieldType=\"Normal\">"
            + "<AutoEnter value=\"CreationDate\"/></Field>");
        Assert.Equal(AutoEnterType.CreationDate, f.AutoEnter);
    }

    [Fact]
    public void FromXml_ValidationNotEmpty()
    {
        var f = Parse(
            "<Field id=\"10\" name=\"Name\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Validation><NotEmpty value=\"True\"/></Validation></Field>");
        Assert.True(f.NotEmpty);
    }

    [Fact]
    public void FromXml_ValidationUnique()
    {
        var f = Parse(
            "<Field id=\"11\" name=\"Email\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Validation><Unique value=\"True\"/></Validation></Field>");
        Assert.True(f.Unique);
    }

    [Fact]
    public void FromXml_ValidationRange()
    {
        var f = Parse(
            "<Field id=\"12\" name=\"Age\" dataType=\"Number\" fieldType=\"Normal\">"
            + "<Validation><Range><MinimumValue>0</MinimumValue><MaximumValue>150</MaximumValue></Range></Validation></Field>");
        Assert.Equal("0", f.RangeMin);
        Assert.Equal("150", f.RangeMax);
    }

    [Fact]
    public void FromXml_ValidationCalculation()
    {
        var f = Parse(
            "<Field id=\"13\" name=\"Code\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Validation><StrictValidation><![CDATA[Length(Self) = 5]]></StrictValidation></Validation></Field>");
        Assert.Equal("Length(Self) = 5", f.ValidationCalculation);
    }

    [Fact]
    public void FromXml_ValidationMaxLength()
    {
        var f = Parse(
            "<Field id=\"14\" name=\"Notes\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Validation><MaxDataLength length=\"500\"/></Validation></Field>");
        Assert.Equal("500", f.MaxDataLength);
    }

    [Fact]
    public void FromXml_ValidationErrorMessage()
    {
        var f = Parse(
            "<Field id=\"15\" name=\"X\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Validation><ErrorMessage><![CDATA[Please enter a value]]></ErrorMessage></Validation></Field>");
        Assert.Equal("Please enter a value", f.ErrorMessage);
    }

    [Fact]
    public void FromXml_GlobalField()
    {
        var f = Parse(
            "<Field id=\"16\" name=\"G\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Storage global=\"True\"/></Field>");
        Assert.True(f.IsGlobal);
    }

    [Fact]
    public void FromXml_IndexedField()
    {
        var f = Parse(
            "<Field id=\"17\" name=\"I\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Storage indexed=\"All\"/></Field>");
        Assert.Equal(FieldIndexing.All, f.Indexing);
    }

    [Fact]
    public void FromXml_WithComment()
    {
        var f = Parse(
            "<Field id=\"18\" name=\"X\" dataType=\"Text\" fieldType=\"Normal\">"
            + "<Comment>Primary key</Comment></Field>");
        Assert.Equal("Primary key", f.Comment);
    }

    [Fact]
    public void FromXml_WithRepetitions()
    {
        var f = Parse("<Field id=\"19\" name=\"X\" dataType=\"Text\" fieldType=\"Normal\" maxRepetition=\"5\"/>");
        Assert.Equal(5, f.Repetitions);
    }

    [Fact]
    public void ToXml_NormalField()
    {
        var f = new FmField { Id = 1, Name = "Test", DataType = FieldDataType.Text, Kind = FieldKind.Normal };
        var xml = f.ToXml();
        Assert.Equal("Test", xml.Attribute("name")?.Value);
        Assert.Equal("Text", xml.Attribute("dataType")?.Value);
        Assert.Equal("Normal", xml.Attribute("fieldType")?.Value);
    }

    [Fact]
    public void ToXml_CalculatedField()
    {
        var f = new FmField
        {
            Id = 2, Name = "Total", DataType = FieldDataType.Number,
            Kind = FieldKind.Calculated, Calculation = "Qty * Price"
        };
        var xml = f.ToXml();
        Assert.Equal("Calculated", xml.Attribute("fieldType")?.Value);
        Assert.Equal("Qty * Price", xml.Element("Calculation")?.Value);
    }

    [Fact]
    public void ToXml_SummaryField()
    {
        var f = new FmField
        {
            Id = 3, Name = "Sum", DataType = FieldDataType.Number,
            Kind = FieldKind.Summary, SummaryOp = SummaryOperation.Sum,
            SummaryTargetField = "Items::Amount"
        };
        var xml = f.ToXml();
        var summary = xml.Element("SummaryField");
        Assert.NotNull(summary);
        Assert.Equal("Sum", summary!.Attribute("operation")?.Value);
    }

    [Fact]
    public void ToXml_FieldWithAutoEnter()
    {
        var f = new FmField
        {
            Id = 4, Name = "ID", DataType = FieldDataType.Number,
            AutoEnter = AutoEnterType.Serial, AutoEnterValue = "100"
        };
        var xml = f.ToXml();
        var autoEl = xml.Element("AutoEnter");
        Assert.NotNull(autoEl);
        Assert.Equal("100", autoEl!.Element("Serial")?.Attribute("nextSerialNumber")?.Value);
    }

    [Fact]
    public void ToXml_FieldWithValidation()
    {
        var f = new FmField
        {
            Id = 5, Name = "Email", DataType = FieldDataType.Text,
            NotEmpty = true, Unique = true, ErrorMessage = "Required"
        };
        var xml = f.ToXml();
        var valEl = xml.Element("Validation");
        Assert.NotNull(valEl);
        Assert.Equal("True", valEl!.Element("NotEmpty")?.Attribute("value")?.Value);
        Assert.Equal("True", valEl.Element("Unique")?.Attribute("value")?.Value);
        Assert.Equal("Required", valEl.Element("ErrorMessage")?.Value);
    }

    [Fact]
    public void ToXml_FieldWithStorage()
    {
        var f = new FmField
        {
            Id = 6, Name = "G", DataType = FieldDataType.Text,
            IsGlobal = true, Indexing = FieldIndexing.Minimal
        };
        var xml = f.ToXml();
        var storage = xml.Element("Storage");
        Assert.NotNull(storage);
        Assert.Equal("True", storage!.Attribute("global")?.Value);
        Assert.Equal("Minimal", storage.Attribute("indexed")?.Value);
    }
}
