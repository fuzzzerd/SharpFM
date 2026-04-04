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
    public void ToXml_ReturnsSavedState_NotLiveBuffer()
    {
        var editor = new FallbackXmlEditor("<root/>");
        editor.Document.Text = "<modified/>";

        // ToXml returns saved state, not live buffer
        Assert.Equal("<root/>", editor.ToXml());

        // After save, ToXml returns the updated content
        editor.Save();
        Assert.Equal("<modified/>", editor.ToXml());
    }

    [Fact]
    public void IsDirty_TracksUnsavedChanges()
    {
        var editor = new FallbackXmlEditor("<root/>");
        Assert.False(editor.IsDirty);

        editor.Document.Text = "<modified/>";
        Assert.True(editor.IsDirty);

        editor.Save();
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void FromXml_ClearsDirtyState()
    {
        var editor = new FallbackXmlEditor("<root/>");
        editor.Document.Text = "<modified/>";
        Assert.True(editor.IsDirty);

        editor.FromXml("<external/>");
        Assert.False(editor.IsDirty);
        Assert.Equal("<external/>", editor.Document.Text);
    }

    [Fact]
    public void BecameDirty_FiresOnce()
    {
        var editor = new FallbackXmlEditor("<root/>");
        int fireCount = 0;
        editor.BecameDirty += (_, _) => fireCount++;

        editor.Document.Text = "<a/>";
        editor.Document.Text = "<b/>";

        Assert.Equal(1, fireCount);
    }

    [Fact]
    public void Saved_FiresOnSave()
    {
        var editor = new FallbackXmlEditor("<root/>");
        var fired = false;
        editor.Saved += (_, _) => fired = true;

        editor.Document.Text = "<modified/>";
        editor.Save();

        Assert.True(fired);
    }

    [Fact]
    public void FromXml_SetsDocumentText()
    {
        var editor = new FallbackXmlEditor("<original/>");
        editor.FromXml("<updated/>");

        Assert.Equal("<updated/>", editor.Document.Text);
    }

    [Fact]
    public void IsPartial_FalseForValidXml()
    {
        var editor = new FallbackXmlEditor("<root/>");
        Assert.False(editor.IsPartial);
    }

    [Fact]
    public void Save_InvalidXml_ReturnsFalse()
    {
        var editor = new FallbackXmlEditor("<root/>");
        editor.Document.Text = "<unclosed";

        Assert.False(editor.Save());
        Assert.True(editor.IsPartial);
        Assert.True(editor.IsDirty); // stays dirty
    }

    [Fact]
    public void Save_InvalidXml_PreservesLastGoodState()
    {
        var editor = new FallbackXmlEditor("<root/>");
        editor.Document.Text = "<unclosed";
        editor.Save();

        // ToXml still returns the last valid saved state
        Assert.Equal("<root/>", editor.ToXml());
    }

    [Fact]
    public void Save_InvalidThenValid_Recovers()
    {
        var editor = new FallbackXmlEditor("<root/>");

        // First: invalid save fails
        editor.Document.Text = "not xml at all";
        Assert.False(editor.Save());
        Assert.True(editor.IsPartial);

        // Second: valid save succeeds
        editor.Document.Text = "<fixed/>";
        Assert.True(editor.Save());
        Assert.False(editor.IsPartial);
        Assert.Equal("<fixed/>", editor.ToXml());
    }

    [Fact]
    public void Save_InvalidXml_DoesNotFireSavedEvent()
    {
        var editor = new FallbackXmlEditor("<root/>");
        var fired = false;
        editor.Saved += (_, _) => fired = true;

        editor.Document.Text = "<broken";
        editor.Save();

        Assert.False(fired);
    }

    [Fact]
    public void Constructor_HandlesNullXml()
    {
        var editor = new FallbackXmlEditor(null);
        Assert.Equal("", editor.Document.Text);
    }

    [Fact]
    public void ToXml_FromXml_RoundTrip()
    {
        var xml = "<fmxmlsnippet type=\"FMObjectList\"><Layout name=\"Test\"/></fmxmlsnippet>";
        var editor = new FallbackXmlEditor(xml);

        var exported = editor.ToXml();
        editor.FromXml(exported);

        Assert.Equal(exported, editor.Document.Text);
    }
}
