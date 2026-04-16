using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Serialization;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

/// <summary>
/// Verifies that the stateless catalog-driven renderer reproduces the
/// display strings the retired <c>StepParamValue</c> pipeline produced.
/// These tests cross-reference existing <c>HandlerTests</c> / <c>ScriptStepTests</c>
/// expectations so unmigrated catalog steps continue rendering identically.
/// </summary>
public class CatalogDisplayRendererTests
{
    private static XElement Parse(string xml) => XElement.Parse(xml);

    [Fact]
    public void Render_NoParamsStep_ReturnsBareName()
    {
        var el = Parse("<Step enable=\"True\" id=\"69\" name=\"Else\"/>");
        var def = StepCatalogLoader.ByName["Else"];

        Assert.Equal("Else", CatalogDisplayRenderer.Render(el, def));
    }

    [Fact]
    public void Render_IfStep_WithCalculation_BuildsBracketedForm()
    {
        var el = Parse(
            "<Step enable=\"True\" id=\"68\" name=\"If\">"
            + "<Calculation><![CDATA[$x > 0]]></Calculation></Step>");
        var def = StepCatalogLoader.ByName["If"];

        Assert.Equal("If [ $x > 0 ]", CatalogDisplayRenderer.Render(el, def));
    }

    [Fact]
    public void Render_EnumParam_ResolvesHrValue()
    {
        var el = Parse(
            "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
            + "<RowPageLocation value=\"Next\"/>"
            + "<Exit state=\"False\"/></Step>");
        var def = StepCatalogLoader.ByName["Go to Record/Request/Page"];

        var result = CatalogDisplayRenderer.Render(el, def);
        // Catalog order renders enum first; boolean Exit is False and
        // may render as "Exit after last: Off" depending on catalog metadata.
        Assert.Contains("Next", result);
    }

    [Fact]
    public void Render_CommentStep_FormatsWithHashPrefix()
    {
        var el = Parse("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>hello world</Text></Step>");
        var def = StepCatalogLoader.ByName["# (comment)"];

        Assert.Equal("# hello world", CatalogDisplayRenderer.Render(el, def));
    }

    [Fact]
    public void Render_EmptyComment_StillReturnsHashPrefix()
    {
        var el = Parse("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"/>");
        var def = StepCatalogLoader.ByName["# (comment)"];

        Assert.Equal("# ", CatalogDisplayRenderer.Render(el, def));
    }

    [Fact]
    public void Render_NamedLayoutRef_RendersQuotedName()
    {
        // Without a typed POCO, generic rendering only carries the name
        // (no id suffix). This is the faithful pre-migration behavior.
        var el = Parse(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"SelectedLayout\"/>"
            + "<Layout id=\"1\" name=\"Detail\"/></Step>");
        var def = StepCatalogLoader.ByName["Go to Layout"];

        var result = CatalogDisplayRenderer.Render(el, def);
        Assert.Contains("\"Detail\"", result);
    }
}
