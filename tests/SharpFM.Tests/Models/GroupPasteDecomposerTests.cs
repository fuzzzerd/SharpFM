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

        var entries = GroupPasteDecomposer.TryDecompose(xml);

        Assert.NotNull(entries);
        var entry = Assert.Single(entries!);
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

        var entries = GroupPasteDecomposer.TryDecompose(xml);

        Assert.NotNull(entries);
        Assert.Equal(3, entries!.Count);
        Assert.All(entries, e => Assert.Equal(new[] { "Utilities" }, e.FolderPath));
        Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, entries.Select(e => e.Name));
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

        var entries = GroupPasteDecomposer.TryDecompose(xml);

        Assert.NotNull(entries);
        Assert.Equal(2, entries!.Count);
        var outer = entries.Single(e => e.Name == "OuterScript");
        Assert.Equal(new[] { "Outer" }, outer.FolderPath);
        var inner = entries.Single(e => e.Name == "InnerScript");
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

        var entries = GroupPasteDecomposer.TryDecompose(xml);

        Assert.NotNull(entries);
        Assert.Equal(2, entries!.Count);
        var loose = entries.Single(e => e.Name == "Loose");
        Assert.Empty(loose.FolderPath);
        var inside = entries.Single(e => e.Name == "Inside");
        Assert.Equal(new[] { "Folder" }, inside.FolderPath);
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

        var entries = GroupPasteDecomposer.TryDecompose(xml)!;
        var entry = Assert.Single(entries);
        var doc = XDocument.Parse(entry.Xml);
        Assert.Equal("FMObjectList", doc.Root!.Attribute("type")?.Value);
        Assert.NotNull(doc.Root.Element("Script")!.Element("Step"));
    }
}
