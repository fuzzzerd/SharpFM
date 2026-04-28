using SharpFM.Editors;
using SharpFM.Model.Schema;
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

    private static TableClipEditor MakeEditor(string? xml) =>
        new(FmTable.FromXml(xml ?? ""));

    [Fact]
    public void Constructor_ParsesFields()
    {
        var editor = MakeEditor(SampleTableXml);

        Assert.Equal(2, editor.ViewModel.Fields.Count);
        Assert.Equal("FirstName", editor.ViewModel.Fields[0].Name);
        Assert.Equal("LastName", editor.ViewModel.Fields[1].Name);
    }

    [Fact]
    public void ToXml_RoundTrips()
    {
        var editor = MakeEditor(SampleTableXml);
        var xml = editor.ToXml();

        Assert.Contains("People", xml);
        Assert.Contains("FirstName", xml);
        Assert.Contains("LastName", xml);
        Assert.False(editor.IsPartial);
    }

    [Fact]
    public void ToXml_ReflectsAddedField()
    {
        var editor = MakeEditor(SampleTableXml);
        editor.ViewModel.AddField();

        var xml = editor.ToXml();

        Assert.Contains("NewField", xml);
        Assert.Equal(3, editor.ViewModel.Fields.Count);
    }

    [Fact]
    public void Constructor_HandlesEmptyTable()
    {
        var editor = new TableClipEditor(new FmTable(""));

        Assert.NotNull(editor.ViewModel);
        Assert.Empty(editor.ViewModel.Fields);
    }

    [Fact]
    public void IsPartial_AlwaysFalse()
    {
        var editor = MakeEditor(SampleTableXml);
        editor.ToXml();
        Assert.False(editor.IsPartial);
    }
}
