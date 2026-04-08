using System.Xml.Linq;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Values;

public class FieldRefTests
{
    [Fact]
    public void FromXml_TableQualifiedField_ReadsAllAttributes()
    {
        var el = XElement.Parse("<Field table=\"People\" id=\"1\" name=\"FirstName\"/>");
        var r = FieldRef.FromXml(el);

        Assert.False(r.IsVariable);
        Assert.Equal("People", r.Table);
        Assert.Equal(1, r.Id);
        Assert.Equal("FirstName", r.Name);
    }

    [Fact]
    public void FromXml_BareFieldName_YieldsNullTable()
    {
        var el = XElement.Parse("<Field name=\"MyField\"/>");
        var r = FieldRef.FromXml(el);

        Assert.Null(r.Table);
        Assert.Equal("MyField", r.Name);
    }

    [Fact]
    public void FromXml_EmptyTableAttribute_NormalizesToNull()
    {
        var el = XElement.Parse("<Field table=\"\" id=\"0\" name=\"MyField\"/>");
        var r = FieldRef.FromXml(el);

        Assert.Null(r.Table);
    }

    [Fact]
    public void FromXml_Variable_NoAttributes_ReadsTextAsVariable()
    {
        var el = XElement.Parse("<Field>$count</Field>");
        var r = FieldRef.FromXml(el);

        Assert.True(r.IsVariable);
        Assert.Equal("$count", r.VariableName);
    }

    [Fact]
    public void ToXml_TableQualifiedField_EmitsAttributes()
    {
        var r = FieldRef.ForField("People", 1, "FirstName");
        var el = r.ToXml("Field");

        Assert.Equal("People", el.Attribute("table")!.Value);
        Assert.Equal("1", el.Attribute("id")!.Value);
        Assert.Equal("FirstName", el.Attribute("name")!.Value);
    }

    [Fact]
    public void ToXml_Variable_EmitsElementText()
    {
        var r = FieldRef.ForVariable("$count");
        var el = r.ToXml("Field");

        Assert.Equal("$count", el.Value);
        Assert.Null(el.Attribute("table"));
        Assert.Null(el.Attribute("name"));
    }

    [Fact]
    public void ToDisplayString_TableQualified_UsesDoubleColonSeparator()
    {
        var r = FieldRef.ForField("People", 1, "FirstName");
        Assert.Equal("People::FirstName", r.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_BareField_OmitsTable()
    {
        var r = FieldRef.ForField(null, 0, "MyField");
        Assert.Equal("MyField", r.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_Variable_ReturnsName()
    {
        var r = FieldRef.ForVariable("$count");
        Assert.Equal("$count", r.ToDisplayString());
    }

    [Fact]
    public void RoundTrip_TableQualifiedField_PreservesAllFields()
    {
        var original = XElement.Parse("<Field table=\"People\" id=\"7\" name=\"Email\"/>");
        var r = FieldRef.FromXml(original);
        var roundTripped = r.ToXml("Field");

        Assert.Equal("People", roundTripped.Attribute("table")!.Value);
        Assert.Equal("7", roundTripped.Attribute("id")!.Value);
        Assert.Equal("Email", roundTripped.Attribute("name")!.Value);
    }
}
