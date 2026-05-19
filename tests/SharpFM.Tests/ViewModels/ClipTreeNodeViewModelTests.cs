using System.Linq;
using SharpFM.Model;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class ClipTreeNodeViewModelTests
{
    private static ClipViewModel Clip(string name, params string[] folderPath)
    {
        var vm = new ClipViewModel(SharpFM.Model.Clip.FromXml(name, "Mac-XMSS",
            "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>"));
        vm.FolderPath = folderPath;
        return vm;
    }

    [Fact]
    public void Build_FlatClips_ProducesRootLeaves()
    {
        var clips = new[] { Clip("A"), Clip("B") };
        var nodes = ClipTreeNodeViewModel.Build(clips);

        Assert.Equal(2, nodes.Count);
        Assert.All(nodes, n => Assert.True(n.IsClip));
        Assert.Equal(new[] { "A", "B" }, nodes.Select(n => n.Name));
    }

    [Fact]
    public void Build_NestedClips_GroupsUnderFolders()
    {
        var clips = new[]
        {
            Clip("Top"),
            Clip("Inner", "Scripts"),
            Clip("Deep", "Scripts", "Utils")
        };

        var nodes = ClipTreeNodeViewModel.Build(clips);

        // Folder "Scripts" first (folders before leaves at root).
        Assert.Equal(2, nodes.Count);
        var scripts = nodes[0];
        Assert.True(scripts.IsFolder);
        Assert.Equal("Scripts", scripts.Name);

        var innerLeaf = scripts.Children.Single(c => c.IsClip);
        Assert.Equal("Inner", innerLeaf.Name);

        var utils = scripts.Children.Single(c => c.IsFolder);
        Assert.Equal("Utils", utils.Name);
        Assert.Equal("Deep", utils.Children.Single().Name);

        Assert.True(nodes[1].IsClip);
        Assert.Equal("Top", nodes[1].Name);
    }

    [Fact]
    public void Build_Search_FiltersClipsAndExpandsMatchingFolders()
    {
        var clips = new[]
        {
            Clip("Hidden", "Scripts"),
            Clip("NeedleClip", "Scripts", "Utils"),
            Clip("RootMatch")
        };

        var nodes = ClipTreeNodeViewModel.Build(clips, "needle");

        // Hidden clip filtered out; Scripts folder survives because a
        // descendant matches; its "Utils" subfolder is auto-expanded.
        var scripts = nodes.Single(n => n.IsFolder);
        Assert.True(scripts.IsExpanded);
        var utils = scripts.Children.Single();
        Assert.True(utils.IsFolder);
        Assert.True(utils.IsExpanded);
        Assert.Equal("NeedleClip", utils.Children.Single().Name);

        // Unrelated root clip drops because it doesn't match.
        Assert.DoesNotContain(nodes, n => n.IsClip && n.Name == "RootMatch");
    }

    [Fact]
    public void Build_EmptyFolder_RendersAsFolderNode()
    {
        var folders = new[] { new FolderData(new[] { "Empty" }) };

        var nodes = ClipTreeNodeViewModel.Build([], folders: folders);

        var empty = Assert.Single(nodes);
        Assert.True(empty.IsFolder);
        Assert.Equal("Empty", empty.Name);
        Assert.Equal(new[] { "Empty" }, empty.Path);
        Assert.Empty(empty.Children);
    }

    [Fact]
    public void Build_NestedFolderNode_CarriesFullPath()
    {
        var folders = new[] { new FolderData(new[] { "Outer", "Inner" }) };

        var nodes = ClipTreeNodeViewModel.Build([], folders: folders);

        var outer = Assert.Single(nodes);
        Assert.Equal(new[] { "Outer" }, outer.Path);
        var inner = outer.Children.Single();
        Assert.Equal(new[] { "Outer", "Inner" }, inner.Path);
    }

    [Fact]
    public void Build_ClipDerivedFolder_CarriesPath()
    {
        var clips = new[] { Clip("X", "Outer", "Inner") };

        var nodes = ClipTreeNodeViewModel.Build(clips);

        var outer = Assert.Single(nodes);
        Assert.Equal(new[] { "Outer" }, outer.Path);
        var inner = outer.Children.Single(c => c.IsFolder);
        Assert.Equal(new[] { "Outer", "Inner" }, inner.Path);
    }

    [Fact]
    public void Build_EmptyFolderInsideClipFolder_RendersAsChild()
    {
        var clips = new[] { Clip("Sibling", "Scripts") };
        var folders = new[] { new FolderData(new[] { "Scripts", "Drafts" }) };

        var nodes = ClipTreeNodeViewModel.Build(clips, folders: folders);

        var scripts = Assert.Single(nodes);
        var drafts = scripts.Children.Single(c => c.IsFolder);
        Assert.Equal("Drafts", drafts.Name);
        Assert.Empty(drafts.Children);
    }

    [Fact]
    public void Build_FilterActive_HidesEmptyFolders()
    {
        var folders = new[] { new FolderData(new[] { "Empty" }) };

        var nodes = ClipTreeNodeViewModel.Build([], "needle", folders);

        Assert.Empty(nodes);
    }

    [Fact]
    public void Build_IsStable_WhenFolderPathCasingDiffers()
    {
        var clips = new[]
        {
            Clip("A", "Scripts"),
            Clip("B", "scripts")
        };

        var nodes = ClipTreeNodeViewModel.Build(clips);

        var folder = Assert.Single(nodes, n => n.IsFolder);
        Assert.Equal(2, folder.Children.Count);
    }
}
