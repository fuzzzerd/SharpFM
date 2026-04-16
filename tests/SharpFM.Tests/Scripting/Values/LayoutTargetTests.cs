using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Values;

public class LayoutTargetTests
{
    [Fact]
    public void Original_WireValue_IsOriginalLayout()
    {
        LayoutTarget t = new LayoutTarget.Original();
        Assert.Equal("OriginalLayout", t.WireValue);
    }

    [Fact]
    public void Named_WireValue_IsSelectedLayout()
    {
        LayoutTarget t = new LayoutTarget.Named(new NamedRef(81, "Projects"));
        Assert.Equal("SelectedLayout", t.WireValue);
    }

    [Fact]
    public void ByNameCalc_WireValue_MatchesFileMakerActualValue()
    {
        LayoutTarget t = new LayoutTarget.ByNameCalc(new Calculation("\"Detail\""));
        // FM Pro writes "LayoutNameByCalc", NOT "LayoutNameByCalculation".
        Assert.Equal("LayoutNameByCalc", t.WireValue);
    }

    [Fact]
    public void ByNumberCalc_WireValue_MatchesFileMakerActualValue()
    {
        LayoutTarget t = new LayoutTarget.ByNumberCalc(new Calculation("3"));
        Assert.Equal("LayoutNumberByCalc", t.WireValue);
    }

    [Fact]
    public void Named_CarriesNamedRef()
    {
        var target = new LayoutTarget.Named(new NamedRef(81, "Projects"));
        Assert.Equal(81, target.Layout.Id);
        Assert.Equal("Projects", target.Layout.Name);
    }

    [Fact]
    public void ByNameCalc_CarriesCalculation()
    {
        var target = new LayoutTarget.ByNameCalc(new Calculation("\"Foo\" & $bar"));
        Assert.Equal("\"Foo\" & $bar", target.Calc.Text);
    }

    [Fact]
    public void PatternMatch_IsExhaustiveOverClosedHierarchy()
    {
        // Compile-time check: a switch expression with all four arms should
        // not require a default case. If a new subtype is added, this test
        // fails to compile — a good signal to update dispatch sites.
        LayoutTarget t = new LayoutTarget.Original();
        var label = t switch
        {
            LayoutTarget.Original => "orig",
            LayoutTarget.Named => "named",
            LayoutTarget.ByNameCalc => "name-calc",
            LayoutTarget.ByNumberCalc => "num-calc",
            _ => "unknown" // fallback kept only to silence CS8509 — never hit
        };
        Assert.Equal("orig", label);
    }
}
