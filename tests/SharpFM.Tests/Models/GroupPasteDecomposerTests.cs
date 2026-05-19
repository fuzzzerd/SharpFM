using System.Linq;
using System.Xml.Linq;
using SharpFM.Model;
using Xunit;

namespace SharpFM.Tests.Models;

public class GroupPasteDecomposerTests
{
    [Fact]
    public void NoGroups_ReturnsNull()
    {
        var xml = "<fmxmlsnippet type=\"FMObjectList\"><Script name=\"Solo\" id=\"1\"/></fmxmlsnippet>";
        Assert.Null(GroupPasteDecomposer.TryDecompose(xml));
    }

    [Fact]
    public void SingleGroupWithOneScript_EmitsOneEntryUnderFolder()
    {
        var xml = """
            <fmxmlsnippet type="FMObjectList">
              <Group groupCollapsed="False" includeInMenu="False" id="16" name="Paste Targets">
                <Script includeInMenu="False" runFullAccess="False" id="19" name="FizzBuzz">
                  <Step enable="True" id="141" name="Set Variable"/>
                </Script>
              </Group>
            </fmxmlsnippet>
            """;

        var result = GroupPasteDecomposer.TryDecompose(xml);

        Assert.NotNull(result);
        var entry = Assert.Single(result!.Entries);
        Assert.Equal("FizzBuzz", entry.Name);
        Assert.Equal(new[] { "Paste Targets" }, entry.FolderPath);

        var doc = XDocument.Parse(entry.Xml);
        Assert.Equal("fmxmlsnippet", doc.Root!.Name.LocalName);
        var script = doc.Root.Element("Script")!;
        Assert.Equal("FizzBuzz", script.Attribute("name")!.Value);
        Assert.Equal("19", script.Attribute("id")!.Value);
        Assert.Null(doc.Root.Element("Group"));
    }

    [Fact]
    public void MultipleScriptsInGroup_EmitsOneEntryPerScript()
    {
        var xml = """
            <fmxmlsnippet type="FMObjectList">
              <Group id="1" name="Utilities">
                <Script id="10" name="Alpha"/>
                <Script id="11" name="Beta"/>
                <Script id="12" name="Gamma"/>
              </Group>
            </fmxmlsnippet>
            """;

        var result = GroupPasteDecomposer.TryDecompose(xml);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Entries.Count);
        Assert.All(result.Entries, e => Assert.Equal(new[] { "Utilities" }, e.FolderPath));
        Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, result.Entries.Select(e => e.Name));
    }

    [Fact]
    public void NestedGroups_ProduceNestedFolders()
    {
        var xml = """
            <fmxmlsnippet type="FMObjectList">
              <Group id="1" name="Outer">
                <Script id="10" name="OuterScript"/>
                <Group id="2" name="Inner">
                  <Script id="20" name="InnerScript"/>
                </Group>
              </Group>
            </fmxmlsnippet>
            """;

        var result = GroupPasteDecomposer.TryDecompose(xml);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Entries.Count);
        var outer = result.Entries.Single(e => e.Name == "OuterScript");
        Assert.Equal(new[] { "Outer" }, outer.FolderPath);
        var inner = result.Entries.Single(e => e.Name == "InnerScript");
        Assert.Equal(new[] { "Outer", "Inner" }, inner.FolderPath);
    }

    [Fact]
    public void LooseScriptAlongsideGroup_StaysAtRoot()
    {
        var xml = """
            <fmxmlsnippet type="FMObjectList">
              <Script id="5" name="Loose"/>
              <Group id="1" name="Folder">
                <Script id="10" name="Inside"/>
              </Group>
            </fmxmlsnippet>
            """;

        var result = GroupPasteDecomposer.TryDecompose(xml);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Entries.Count);
        var loose = result.Entries.Single(e => e.Name == "Loose");
        Assert.Empty(loose.FolderPath);
        var inside = result.Entries.Single(e => e.Name == "Inside");
        Assert.Equal(new[] { "Folder" }, inside.FolderPath);
    }

    [Fact]
    public void Decompose_CapturesGroupAttributes()
    {
        var xml = """
            <fmxmlsnippet type="FMObjectList">
              <Group groupCollapsed="True" includeInMenu="False" id="16" name="Paste Targets">
                <Script id="19" name="FizzBuzz"/>
              </Group>
            </fmxmlsnippet>
            """;

        var result = GroupPasteDecomposer.TryDecompose(xml);

        Assert.NotNull(result);
        var folder = Assert.Single(result!.Folders);
        Assert.Equal(new[] { "Paste Targets" }, folder.Path);
        Assert.Equal(16, folder.Id);
        Assert.False(folder.IncludeInMenu);
        Assert.True(folder.GroupCollapsed);
    }

    [Fact]
    public void Decompose_NestedGroups_EmitsFolderPerLevel()
    {
        var xml = """
            <fmxmlsnippet type="FMObjectList">
              <Group id="1" name="Outer" includeInMenu="True" groupCollapsed="False">
                <Group id="2" name="Inner" includeInMenu="False" groupCollapsed="True">
                  <Script id="20" name="InnerScript"/>
                </Group>
              </Group>
            </fmxmlsnippet>
            """;

        var result = GroupPasteDecomposer.TryDecompose(xml)!;

        Assert.Equal(2, result.Folders.Count);
        var outer = result.Folders.Single(f => f.Path.Count == 1);
        Assert.Equal("Outer", outer.Path[0]);
        Assert.Equal(1, outer.Id);
        Assert.True(outer.IncludeInMenu);
        Assert.False(outer.GroupCollapsed);

        var inner = result.Folders.Single(f => f.Path.Count == 2);
        Assert.Equal(new[] { "Outer", "Inner" }, inner.Path);
        Assert.Equal(2, inner.Id);
        Assert.False(inner.IncludeInMenu);
        Assert.True(inner.GroupCollapsed);
    }

    [Fact]
    public void Decompose_DefaultGroupAttributes_AreSensible()
    {
        var xml = """
            <fmxmlsnippet type="FMObjectList">
              <Group id="9" name="Bare">
                <Script id="10" name="X"/>
              </Group>
            </fmxmlsnippet>
            """;

        var result = GroupPasteDecomposer.TryDecompose(xml)!;
        var folder = Assert.Single(result.Folders);
        Assert.Equal(9, folder.Id);
        Assert.True(folder.IncludeInMenu);
        Assert.False(folder.GroupCollapsed);
    }

    [Fact]
    public void EachEntryXml_WrapsScriptInFmxmlsnippet()
    {
        var xml = """
            <fmxmlsnippet type="FMObjectList">
              <Group id="1" name="G">
                <Script id="10" name="S">
                  <Step enable="True" id="141" name="Set Variable"><Name>$i</Name></Step>
                </Script>
              </Group>
            </fmxmlsnippet>
            """;

        var result = GroupPasteDecomposer.TryDecompose(xml)!;
        var entry = Assert.Single(result.Entries);
        var doc = XDocument.Parse(entry.Xml);
        Assert.Equal("FMObjectList", doc.Root!.Attribute("type")?.Value);
        Assert.NotNull(doc.Root.Element("Script")!.Element("Step"));
    }
}
