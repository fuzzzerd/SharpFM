using System.Xml.Linq;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Values;

public class AnimationTests
{
    [Fact]
    public void FromXml_ReadsValueAttribute()
    {
        var el = XElement.Parse("<Animation value=\"SlideFromLeft\"/>");
        var a = Animation.FromXml(el);

        Assert.Equal("SlideFromLeft", a.WireValue);
    }

    [Fact]
    public void FromXml_MissingValueAttribute_YieldsEmptyString()
    {
        var el = XElement.Parse("<Animation/>");
        var a = Animation.FromXml(el);

        Assert.Equal("", a.WireValue);
    }

    [Fact]
    public void FromXml_UnknownWireValue_RoundTripsUnchanged()
    {
        // The enum-less design intentionally allows unknown/future FM
        // values to pass through untouched rather than being coerced or lost.
        var el = XElement.Parse("<Animation value=\"SomeFutureTransition\"/>");
        var a = Animation.FromXml(el);

        Assert.Equal("SomeFutureTransition", a.WireValue);
    }

    [Fact]
    public void ToXml_EmitsValueAttribute()
    {
        var a = new Animation("CrossDissolve");
        var el = a.ToXml();

        Assert.Equal("Animation", el.Name.LocalName);
        Assert.Equal("CrossDissolve", el.Attribute("value")!.Value);
    }

    [Fact]
    public void None_Convenience_ReturnsNoneAnimation()
    {
        Assert.Equal("None", Animation.None.WireValue);
    }

    [Fact]
    public void RoundTrip_PreservesWireValue()
    {
        var original = XElement.Parse("<Animation value=\"SlideToLeft\"/>");
        var a = Animation.FromXml(original);
        var roundTripped = a.ToXml();

        Assert.Equal(original.Attribute("value")!.Value,
            roundTripped.Attribute("value")!.Value);
    }

    [Fact]
    public void RecordEquality_SameWireValue_AreEqual()
    {
        // Records give us value equality for free, which matters for test
        // assertions and for clean semantic comparisons on the domain side.
        Assert.Equal(new Animation("None"), new Animation("None"));
        Assert.NotEqual(new Animation("None"), new Animation("SlideFromLeft"));
    }
}
