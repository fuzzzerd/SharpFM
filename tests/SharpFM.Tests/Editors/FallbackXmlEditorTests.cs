using SharpFM.Editors;
using Xunit;

namespace SharpFM.Tests.Editors;

public class FallbackXmlEditorTests
{
    [Fact]
    public void Constructor_SetsDocumentText()
    {
        var xml = "<root><child/></root>";
        var editor = new FallbackXmlEditor(xml);

        Assert.Equal(xml, editor.Document.Text);
    }

    [Fact]
    public void ToXml_ReturnsDocumentText()
    {
        var editor = new FallbackXmlEditor("<root/>");
        editor.Document.Text = "<modified/>";

        Assert.Equal("<modified/>", editor.ToXml());
    }

    [Fact]
    public void IsPartial_AlwaysFalse()
    {
        var editor = new FallbackXmlEditor("<root/>");
        Assert.False(editor.IsPartial);
    }

    [Fact]
    public void Constructor_HandlesNullXml()
    {
        var editor = new FallbackXmlEditor(null);
        Assert.Equal("", editor.Document.Text);
    }

    [Fact]
    public void ToXml_RoundTripsViaDocumentText()
    {
        var xml = "<fmxmlsnippet type=\"FMObjectList\"><Layout name=\"Test\"/></fmxmlsnippet>";
        var editor = new FallbackXmlEditor(xml);

        Assert.Equal(xml, editor.ToXml());
    }
}
