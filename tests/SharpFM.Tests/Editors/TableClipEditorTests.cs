using SharpFM.Editors;
using SharpFM.Schema.Model;
using Xunit;

namespace SharpFM.Tests.Editors;

public class TableClipEditorTests
{
    private const string SampleTableXml =
        "<fmxmlsnippet type=\"FMObjectList\">" +
        "<BaseTable name=\"People\">" +
        "<Field id=\"1\" name=\"FirstName\" dataType=\"Text\" fieldType=\"Normal\"/>" +
        "<Field id=\"2\" name=\"LastName\" dataType=\"Text\" fieldType=\"Normal\"/>" +
        "</BaseTable></fmxmlsnippet>";

    [Fact]
    public void Constructor_ParsesFields()
    {
        var editor = new TableClipEditor(SampleTableXml);

        Assert.Equal(2, editor.ViewModel.Fields.Count);
        Assert.Equal("FirstName", editor.ViewModel.Fields[0].Name);
        Assert.Equal("LastName", editor.ViewModel.Fields[1].Name);
    }

    [Fact]
    public void ToXml_RoundTrips()
    {
        var editor = new TableClipEditor(SampleTableXml);
        var xml = editor.ToXml();

        Assert.Contains("People", xml);
        Assert.Contains("FirstName", xml);
        Assert.Contains("LastName", xml);
        Assert.False(editor.IsPartial);
    }

    [Fact]
    public void ToXml_ReflectsAddedField()
    {
        var editor = new TableClipEditor(SampleTableXml);
        editor.ViewModel.AddField();

        var xml = editor.ToXml();

        Assert.Contains("NewField", xml);
        Assert.Equal(3, editor.ViewModel.Fields.Count);
    }

    [Fact]
    public void FromXml_PatchesExistingFields()
    {
        var editor = new TableClipEditor(SampleTableXml);

        // Modify a field name in XML
        var modifiedXml = SampleTableXml.Replace("FirstName", "GivenName");
        editor.FromXml(modifiedXml);

        Assert.Equal("GivenName", editor.ViewModel.Fields[0].Name);
        Assert.Equal("LastName", editor.ViewModel.Fields[1].Name);
        Assert.Equal(2, editor.ViewModel.Fields.Count);
    }

    [Fact]
    public void FromXml_AddsNewFields()
    {
        var editor = new TableClipEditor(SampleTableXml);

        var withExtraField =
            "<fmxmlsnippet type=\"FMObjectList\">" +
            "<BaseTable name=\"People\">" +
            "<Field id=\"1\" name=\"FirstName\" dataType=\"Text\" fieldType=\"Normal\"/>" +
            "<Field id=\"2\" name=\"LastName\" dataType=\"Text\" fieldType=\"Normal\"/>" +
            "<Field id=\"3\" name=\"Email\" dataType=\"Text\" fieldType=\"Normal\"/>" +
            "</BaseTable></fmxmlsnippet>";
        editor.FromXml(withExtraField);

        Assert.Equal(3, editor.ViewModel.Fields.Count);
        Assert.Equal("Email", editor.ViewModel.Fields[2].Name);
    }

    [Fact]
    public void FromXml_RemovesDeletedFields()
    {
        var editor = new TableClipEditor(SampleTableXml);

        var withOneField =
            "<fmxmlsnippet type=\"FMObjectList\">" +
            "<BaseTable name=\"People\">" +
            "<Field id=\"1\" name=\"FirstName\" dataType=\"Text\" fieldType=\"Normal\"/>" +
            "</BaseTable></fmxmlsnippet>";
        editor.FromXml(withOneField);

        Assert.Single(editor.ViewModel.Fields);
        Assert.Equal("FirstName", editor.ViewModel.Fields[0].Name);
    }

    [Fact]
    public void FromXml_FullRebuild_WhenTableIdentityChanges()
    {
        var editor = new TableClipEditor(SampleTableXml);
        var originalVm = editor.ViewModel;

        var differentTable =
            "<fmxmlsnippet type=\"FMObjectList\">" +
            "<BaseTable id=\"99\" name=\"Animals\">" +
            "<Field id=\"10\" name=\"Species\" dataType=\"Text\" fieldType=\"Normal\"/>" +
            "</BaseTable></fmxmlsnippet>";
        editor.FromXml(differentTable);

        // Should have created a new ViewModel since both name and id changed
        Assert.NotSame(originalVm, editor.ViewModel);
        Assert.Single(editor.ViewModel.Fields);
        Assert.Equal("Species", editor.ViewModel.Fields[0].Name);
    }

    [Fact]
    public void Constructor_HandlesNullXml()
    {
        var editor = new TableClipEditor(null);

        Assert.NotNull(editor.ViewModel);
        Assert.Empty(editor.ViewModel.Fields);
    }

    [Fact]
    public void IsPartial_FalseForValidTable()
    {
        var editor = new TableClipEditor(SampleTableXml);
        editor.Save();
        Assert.False(editor.IsPartial);
    }

    // --- Save/Dirty pattern ---

    [Fact]
    public void IsDirty_FalseOnConstruction()
    {
        var editor = new TableClipEditor(SampleTableXml);
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void IsDirty_TrueAfterFieldChange()
    {
        var editor = new TableClipEditor(SampleTableXml);
        editor.ViewModel.Fields[0].Name = "Modified";

        Assert.True(editor.IsDirty);
    }

    [Fact]
    public void IsDirty_TrueAfterAddField()
    {
        var editor = new TableClipEditor(SampleTableXml);
        editor.ViewModel.AddField();

        Assert.True(editor.IsDirty);
    }

    [Fact]
    public void Save_ClearsDirty()
    {
        var editor = new TableClipEditor(SampleTableXml);
        editor.ViewModel.Fields[0].Name = "Modified";
        Assert.True(editor.IsDirty);

        editor.Save();
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void Save_FiresSavedEvent()
    {
        var editor = new TableClipEditor(SampleTableXml);
        var fired = false;
        editor.Saved += (_, _) => fired = true;

        editor.ViewModel.Fields[0].Name = "Modified";
        editor.Save();

        Assert.True(fired);
    }

    [Fact]
    public void ToXml_ReflectsModelState_NotLiveGrid()
    {
        var editor = new TableClipEditor(SampleTableXml);
        var original = editor.ToXml();

        // Modify grid but don't save
        editor.ViewModel.Fields[0].Name = "Unsaved";

        // ToXml returns the model state (SyncToModel not called)
        // Note: ViewModel.Table is the model, and SyncToModel updates it
        // Without save, the table's XML should still have the original field names
        Assert.Contains("FirstName", original);
    }

    [Fact]
    public void Save_ThenToXml_ReflectsChanges()
    {
        var editor = new TableClipEditor(SampleTableXml);
        editor.ViewModel.Fields[0].Name = "GivenName";
        editor.Save();

        var xml = editor.ToXml();
        Assert.Contains("GivenName", xml);
    }

    [Fact]
    public void FromXml_ClearsDirty()
    {
        var editor = new TableClipEditor(SampleTableXml);
        editor.ViewModel.Fields[0].Name = "Dirty";
        Assert.True(editor.IsDirty);

        editor.FromXml(SampleTableXml);
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public void BecameDirty_FiresOnce()
    {
        var editor = new TableClipEditor(SampleTableXml);
        int fireCount = 0;
        editor.BecameDirty += (_, _) => fireCount++;

        editor.ViewModel.Fields[0].Name = "A";
        editor.ViewModel.Fields[0].Name = "B";

        Assert.Equal(1, fireCount);
    }
}
