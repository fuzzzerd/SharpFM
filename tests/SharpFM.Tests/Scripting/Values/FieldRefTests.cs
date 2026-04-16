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
    public void ToDisplayString_TableQualified_IncludesLosslessIdSuffix()
    {
        var r = FieldRef.ForField("People", 1, "FirstName");
        Assert.Equal("People::FirstName (#1)", r.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_WithIncludeIdFalse_OmitsIdSuffix()
    {
        // Callers that need to insert additional annotation (e.g. a
        // repetition [rep] suffix) between the name and the (#id) can
        // opt out and append the id themselves.
        var r = FieldRef.ForField("People", 1, "FirstName");
        Assert.Equal("People::FirstName", r.ToDisplayString(includeId: false));
    }

    [Fact]
    public void ToDisplayString_UnresolvedId_OmitsSuffix()
    {
        // Id = 0 is the "unresolved" sentinel — emitted as a bare name.
        var r = FieldRef.ForField("People", 0, "FirstName");
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
    public void FromDisplayToken_TableQualifiedWithId_PreservesId()
    {
        var r = FieldRef.FromDisplayToken("People::FirstName (#7)");
        Assert.Equal("People", r.Table);
        Assert.Equal(7, r.Id);
        Assert.Equal("FirstName", r.Name);
        Assert.False(r.IsVariable);
    }

    [Fact]
    public void FromDisplayToken_TableQualifiedNoId_IdIsZero()
    {
        var r = FieldRef.FromDisplayToken("People::FirstName");
        Assert.Equal("People", r.Table);
        Assert.Equal(0, r.Id);
    }

    [Fact]
    public void FromDisplayToken_Variable_IsVariable()
    {
        var r = FieldRef.FromDisplayToken("$count");
        Assert.True(r.IsVariable);
        Assert.Equal("$count", r.VariableName);
    }

    [Fact]
    public void FromDisplayToken_GlobalVariable_IsVariable()
    {
        var r = FieldRef.FromDisplayToken("$$globalCount");
        Assert.True(r.IsVariable);
        Assert.Equal("$$globalCount", r.VariableName);
    }

    [Fact]
    public void FromDisplayToken_BareFieldWithId_PreservesId()
    {
        var r = FieldRef.FromDisplayToken("MyField (#3)");
        Assert.Null(r.Table);
        Assert.Equal(3, r.Id);
        Assert.Equal("MyField", r.Name);
    }

    [Fact]
    public void FromDisplayToken_ThenToDisplayString_IsIdempotent()
    {
        var original = "People::FirstName (#7)";
        var r = FieldRef.FromDisplayToken(original);
        Assert.Equal(original, r.ToDisplayString());
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
