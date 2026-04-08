using System.Xml.Linq;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Values;

public class NamedRefTests
{
    [Fact]
    public void FromXml_ReadsIdAndName()
    {
        var el = XElement.Parse("<Layout id=\"81\" name=\"Projects\"/>");
        var r = NamedRef.FromXml(el);

        Assert.Equal(81, r.Id);
        Assert.Equal("Projects", r.Name);
    }

    [Fact]
    public void FromXml_MissingId_DefaultsToZero()
    {
        var el = XElement.Parse("<Layout name=\"Projects\"/>");
        var r = NamedRef.FromXml(el);

        Assert.Equal(0, r.Id);
        Assert.Equal("Projects", r.Name);
    }

    [Fact]
    public void FromXml_UnparseableId_DefaultsToZero()
    {
        var el = XElement.Parse("<Layout id=\"abc\" name=\"Projects\"/>");
        var r = NamedRef.FromXml(el);

        Assert.Equal(0, r.Id);
    }

    [Fact]
    public void FromXml_MissingName_YieldsEmptyString()
    {
        var el = XElement.Parse("<Layout id=\"1\"/>");
        var r = NamedRef.FromXml(el);

        Assert.Equal("", r.Name);
    }

    [Fact]
    public void ToXml_EmitsIdAndNameAttributes()
    {
        var r = new NamedRef(81, "Projects");
        var el = r.ToXml("Layout");

        Assert.Equal("Layout", el.Name.LocalName);
        Assert.Equal("81", el.Attribute("id")!.Value);
        Assert.Equal("Projects", el.Attribute("name")!.Value);
    }

    [Fact]
    public void ToXml_UsesCallerSuppliedElementName()
    {
        var r = new NamedRef(5, "Init");
        var el = r.ToXml("Script");

        Assert.Equal("Script", el.Name.LocalName);
    }

    [Fact]
    public void RoundTrip_PreservesIdAndName()
    {
        var original = XElement.Parse("<Layout id=\"81\" name=\"Projects\"/>");
        var r = NamedRef.FromXml(original);
        var roundTripped = r.ToXml("Layout");

        Assert.Equal(original.Attribute("id")!.Value, roundTripped.Attribute("id")!.Value);
        Assert.Equal(original.Attribute("name")!.Value, roundTripped.Attribute("name")!.Value);
    }
}
