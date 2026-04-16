using System.Xml.Linq;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Values;

public class CalculationTests
{
    [Fact]
    public void FromXml_ReadsCdataBody()
    {
        var el = XElement.Parse("<Calculation><![CDATA[$x + 1]]></Calculation>");
        var c = Calculation.FromXml(el);

        Assert.Equal("$x + 1", c.Text);
    }

    [Fact]
    public void FromXml_EmptyElement_YieldsEmptyText()
    {
        var el = XElement.Parse("<Calculation></Calculation>");
        var c = Calculation.FromXml(el);

        Assert.Equal("", c.Text);
    }

    [Fact]
    public void ToXml_WrapsTextInCdata()
    {
        var c = new Calculation("$x + 1");
        var el = c.ToXml();

        Assert.Equal("Calculation", el.Name.LocalName);
        Assert.Equal("$x + 1", el.Value);
        // Verify CDATA is present in the serialized form
        Assert.Contains("<![CDATA[$x + 1]]>", el.ToString());
    }

    [Fact]
    public void ToXml_UsesCallerSuppliedElementName()
    {
        var c = new Calculation("true");
        var el = c.ToXml("Condition");

        Assert.Equal("Condition", el.Name.LocalName);
    }

    [Fact]
    public void RoundTrip_PreservesComplexExpression()
    {
        const string expr = "Let ( [ x = 5 ; y = x * 2 ] ; y )";
        var original = XElement.Parse($"<Calculation><![CDATA[{expr}]]></Calculation>");
        var c = Calculation.FromXml(original);
        var roundTripped = c.ToXml();

        Assert.Equal(expr, roundTripped.Value);
    }
}
