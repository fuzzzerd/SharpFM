using System.Xml.Linq;
using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// End-to-end tests for the Go to Layout step, anchored against verbatim
/// FileMaker Pro clipboard XML samples. The goal is lossless round-trip
/// through the typed domain model: every attribute and nested element
/// in the source survives <see cref="ScriptStep.FromXml"/> →
/// <see cref="ScriptStep.ToXml"/> unchanged.
///
/// <para>
/// These tests are expected to fail until the typed <c>GoToLayoutStep</c>
/// POCO lands in Phase 2. In Phase 1 commit 3 (where this file is
/// created) they live as the Phase 2 acceptance criteria.
/// </para>
/// </summary>
public class GoToLayoutStepTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    // Verbatim FM Pro clipboard XML fixtures for each of the four
    // LayoutDestination variants. Do not edit the attribute order or
    // self-closing forms — they reflect what FM Pro actually emits.

    private const string VerbatimSelectedLayoutXml =
        "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
        + "<LayoutDestination value=\"SelectedLayout\"></LayoutDestination>"
        + "<Layout id=\"81\" name=\"Projects\"></Layout>"
        + "<Animation value=\"SlideFromLeft\"></Animation></Step>";

    private const string VerbatimByNameCalcXml =
        "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
        + "<LayoutDestination value=\"LayoutNameByCalc\"></LayoutDestination>"
        + "<Layout><Calculation><![CDATA[\"SomeCALcluLATion\"]]></Calculation></Layout>"
        + "<Animation value=\"CrossDissolve\"></Animation></Step>";

    private const string VerbatimByNumberCalcXml =
        "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
        + "<LayoutDestination value=\"LayoutNumberByCalc\"></LayoutDestination>"
        + "<Layout><Calculation><![CDATA[369*3]]></Calculation></Layout>"
        + "<Animation value=\"SlideToLeft\"></Animation></Step>";

    // OriginalLayout always omits <Layout>. This particular sample
    // also omits <Animation>, but OriginalLayout with an animation set
    // is a valid FM state and must round-trip when present.
    private const string VerbatimOriginalLayoutXml =
        "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
        + "<LayoutDestination value=\"OriginalLayout\"></LayoutDestination></Step>";

    // --- SelectedLayout (named) ---

    [Fact]
    public void SelectedLayout_Display_IncludesLosslessIdSuffix()
    {
        var step = ScriptStep.FromXml(MakeStep(VerbatimSelectedLayoutXml));

        // The (#81) suffix is SharpFM's lossless extension: FM Pro shows
        // the layout's base table in that slot, but deriving the base
        // table requires schema lookup we don't have. Carrying the id
        // ensures nothing is lost on round-trip through display text.
        Assert.Equal(
            "Go to Layout [ \"Projects\" (#81) ; Animation: SlideFromLeft ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void SelectedLayout_RoundTrip_PreservesIdAndAnimation()
    {
        var step = ScriptStep.FromXml(MakeStep(VerbatimSelectedLayoutXml));
        var xml = step.ToXml();

        var layout = xml.Element("Layout");
        Assert.NotNull(layout);
        Assert.Equal("81", layout!.Attribute("id")!.Value);
        Assert.Equal("Projects", layout.Attribute("name")!.Value);

        var animation = xml.Element("Animation");
        Assert.NotNull(animation);
        Assert.Equal("SlideFromLeft", animation!.Attribute("value")!.Value);
    }

    // --- LayoutNameByCalc ---

    [Fact]
    public void ByNameCalc_Display_UsesLayoutNameLabel()
    {
        var step = ScriptStep.FromXml(MakeStep(VerbatimByNameCalcXml));

        Assert.Equal(
            "Go to Layout [ Layout Name: \"SomeCALcluLATion\" ; Animation: CrossDissolve ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void ByNameCalc_RoundTrip_PreservesNestedCalculationAndAnimation()
    {
        var step = ScriptStep.FromXml(MakeStep(VerbatimByNameCalcXml));
        var xml = step.ToXml();

        Assert.Equal("LayoutNameByCalc",
            xml.Element("LayoutDestination")!.Attribute("value")!.Value);

        // The Calculation must stay nested inside <Layout>, matching FM's format.
        var layout = xml.Element("Layout");
        Assert.NotNull(layout);
        Assert.Null(layout!.Attribute("id"));
        Assert.Null(layout.Attribute("name"));
        Assert.Contains("\"SomeCALcluLATion\"",
            layout.Element("Calculation")!.Value);

        Assert.Equal("CrossDissolve",
            xml.Element("Animation")!.Attribute("value")!.Value);
    }

    // --- LayoutNumberByCalc ---

    [Fact]
    public void ByNumberCalc_Display_UsesLayoutNumberLabel()
    {
        var step = ScriptStep.FromXml(MakeStep(VerbatimByNumberCalcXml));

        Assert.Equal(
            "Go to Layout [ Layout Number: 369*3 ; Animation: SlideToLeft ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void ByNumberCalc_RoundTrip_PreservesNestedCalculationAndAnimation()
    {
        var step = ScriptStep.FromXml(MakeStep(VerbatimByNumberCalcXml));
        var xml = step.ToXml();

        Assert.Equal("LayoutNumberByCalc",
            xml.Element("LayoutDestination")!.Attribute("value")!.Value);

        var layout = xml.Element("Layout");
        Assert.NotNull(layout);
        Assert.Null(layout!.Attribute("id"));
        Assert.Null(layout.Attribute("name"));
        Assert.Contains("369*3", layout.Element("Calculation")!.Value);

        Assert.Equal("SlideToLeft",
            xml.Element("Animation")!.Attribute("value")!.Value);
    }

    // --- OriginalLayout ---

    [Fact]
    public void OriginalLayout_Display_HasNoLayoutOrAnimationSuffix()
    {
        var step = ScriptStep.FromXml(MakeStep(VerbatimOriginalLayoutXml));

        Assert.Equal("Go to Layout [ original layout ]", step.ToDisplayLine());
    }

    [Fact]
    public void OriginalLayout_RoundTrip_OmitsLayoutAndAnimationElements()
    {
        var step = ScriptStep.FromXml(MakeStep(VerbatimOriginalLayoutXml));
        var xml = step.ToXml();

        Assert.Equal("OriginalLayout",
            xml.Element("LayoutDestination")!.Attribute("value")!.Value);
        Assert.Null(xml.Element("Layout"));
        Assert.Null(xml.Element("Animation"));
    }

    // --- Full display-text round-trip ---

    [Fact]
    public void SelectedLayout_FullRoundTrip_FromDisplayTextPreservesId()
    {
        // Parse XML → render to display text → parse display text back
        // to XML → verify id and animation survive the whole loop.
        var step1 = ScriptStep.FromXml(MakeStep(VerbatimSelectedLayoutXml));
        var display = step1.ToDisplayLine();

        var script = ScriptFromDisplay(display);
        var rebuilt = script.ToXml();

        var roundTripped = XElement.Parse(rebuilt).Element("Step")!;
        var layout = roundTripped.Element("Layout");
        Assert.NotNull(layout);
        Assert.Equal("81", layout!.Attribute("id")!.Value);
        Assert.Equal("Projects", layout.Attribute("name")!.Value);

        var animation = roundTripped.Element("Animation");
        Assert.NotNull(animation);
        Assert.Equal("SlideFromLeft", animation!.Attribute("value")!.Value);
    }

    private static FmScript ScriptFromDisplay(string display) =>
        SharpFM.Scripting.ScriptTextParser.FromDisplayText(display);
}
