using System.Linq;
using SharpFM.Model;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class ClipTreeNodeViewModelTests
{
    private static ClipViewModel Clip(string name, params string[] folderPath)
    {
        var vm = new ClipViewModel(new FileMakerClip(name, "Mac-XMSS",
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
