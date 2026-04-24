using System.IO;
using System.Linq;
using SharpFM.Models;
using SharpFM.Model;
using Xunit;

namespace SharpFM.Tests.Models;

public class ClipRepositoryTests
{
    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void ProviderName_IsLocalFiles()
    {
        var dir = CreateTempDir();
        try
        {
            var repo = new ClipRepository(dir);
            Assert.Equal("Local Files", repo.ProviderName);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void CurrentLocation_ReturnsPath()
    {
        var dir = CreateTempDir();
        try
        {
            var repo = new ClipRepository(dir);
            Assert.Equal(dir, repo.CurrentLocation);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void SupportsLocationPicker_IsFalse()
    {
        var dir = CreateTempDir();
        try
        {
            var repo = new ClipRepository(dir);
            Assert.False(repo.SupportsLocationPicker);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task PickLocationAsync_ReturnsNull()
    {
        var dir = CreateTempDir();
        try
        {
            var repo = new ClipRepository(dir);
            Assert.Null(await repo.PickLocationAsync());
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Constructor_CreatesDirectory_IfMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"sharpfm-test-{Guid.NewGuid()}");
        try
        {
            Assert.False(Directory.Exists(dir));
            _ = new ClipRepository(dir);
            Assert.True(Directory.Exists(dir));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task LoadClipsAsync_ReturnsEmpty_ForEmptyDir()
    {
        var dir = CreateTempDir();
        try
        {
            var repo = new ClipRepository(dir);
            var clips = await repo.LoadClipsAsync();
            Assert.Empty(clips);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task LoadClipsAsync_ReadsClipFiles()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "MyScript.Mac-XMSS"), "<xml>script</xml>");
            File.WriteAllText(Path.Combine(dir, "MyTable.Mac-XMTB"), "<xml>table</xml>");

            var repo = new ClipRepository(dir);
            var clips = await repo.LoadClipsAsync();

            Assert.Equal(2, clips.Count);
            Assert.Contains(clips, c => c.Name == "MyScript" && c.ClipType == "Mac-XMSS");
            Assert.Contains(clips, c => c.Name == "MyTable" && c.ClipType == "Mac-XMTB");
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task SaveClipsAsync_WritesFiles()
    {
        var dir = CreateTempDir();
        try
        {
            var repo = new ClipRepository(dir);
            var clips = new List<ClipData>
            {
                new("Script1", "Mac-XMSS", "<xml>1</xml>"),
                new("Table1", "Mac-XMTB", "<xml>2</xml>")
            };

            await repo.SaveClipsAsync(clips);

            Assert.True(File.Exists(Path.Combine(dir, "Script1.Mac-XMSS")));
            Assert.True(File.Exists(Path.Combine(dir, "Table1.Mac-XMTB")));
            Assert.Equal("<xml>1</xml>", File.ReadAllText(Path.Combine(dir, "Script1.Mac-XMSS")));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task SaveClipsAsync_DeletesOrphanedFiles()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "Old.Mac-XMSS"), "<old/>");
            File.WriteAllText(Path.Combine(dir, "Keep.Mac-XMSS"), "<keep/>");

            var repo = new ClipRepository(dir);
            await repo.SaveClipsAsync([new("Keep", "Mac-XMSS", "<updated/>")]);

            Assert.False(File.Exists(Path.Combine(dir, "Old.Mac-XMSS")));
            Assert.True(File.Exists(Path.Combine(dir, "Keep.Mac-XMSS")));
            Assert.Equal("<updated/>", File.ReadAllText(Path.Combine(dir, "Keep.Mac-XMSS")));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Roundtrip_LoadSaveLoad()
    {
        var dir = CreateTempDir();
        try
        {
            var repo = new ClipRepository(dir);
            var original = new List<ClipData>
            {
                new("Script", "Mac-XMSS", "<xml>data</xml>")
            };

            await repo.SaveClipsAsync(original);
            var loaded = await repo.LoadClipsAsync();

            Assert.Single(loaded);
            Assert.Equal("Script", loaded[0].Name);
            Assert.Equal("Mac-XMSS", loaded[0].ClipType);
            Assert.Equal("<xml>data</xml>", loaded[0].Xml);
            Assert.Empty(loaded[0].FolderPath);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task LoadClipsAsync_Recurses_IntoSubdirectories()
    {
        var dir = CreateTempDir();
        try
        {
            var sub = Path.Combine(dir, "Scripts", "Utilities");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "Log.Mac-XMSS"), "<xml/>");
            File.WriteAllText(Path.Combine(dir, "RootClip.Mac-XMTB"), "<xml/>");

            var repo = new ClipRepository(dir);
            var clips = await repo.LoadClipsAsync();

            var nested = Assert.Single(clips, c => c.Name == "Log");
            Assert.Equal(new[] { "Scripts", "Utilities" }, nested.FolderPath);

            var rooted = Assert.Single(clips, c => c.Name == "RootClip");
            Assert.Empty(rooted.FolderPath);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task SaveClipsAsync_RoundTripsFolderPath()
    {
        var dir = CreateTempDir();
        try
        {
            var repo = new ClipRepository(dir);
            var clips = new List<ClipData>
            {
                new("A", "Mac-XMSS", "<a/>") { FolderPath = new[] { "Group1", "Sub" } },
                new("B", "Mac-XMTB", "<b/>") { FolderPath = Array.Empty<string>() }
            };

            await repo.SaveClipsAsync(clips);
            Assert.True(File.Exists(Path.Combine(dir, "Group1", "Sub", "A.Mac-XMSS")));
            Assert.True(File.Exists(Path.Combine(dir, "B.Mac-XMTB")));

            var loaded = await repo.LoadClipsAsync();
            var a = Assert.Single(loaded, c => c.Name == "A");
            Assert.Equal(new[] { "Group1", "Sub" }, a.FolderPath);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task SaveClipsAsync_DeletesOrphans_AcrossSubdirectories()
    {
        var dir = CreateTempDir();
        try
        {
            var sub = Path.Combine(dir, "Old");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "Gone.Mac-XMSS"), "<x/>");

            var repo = new ClipRepository(dir);
            await repo.SaveClipsAsync([new("Keep", "Mac-XMSS", "<k/>")]);

            Assert.False(File.Exists(Path.Combine(sub, "Gone.Mac-XMSS")));
            Assert.False(Directory.Exists(sub));
            Assert.True(File.Exists(Path.Combine(dir, "Keep.Mac-XMSS")));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task SaveClipsAsync_RejectsTraversalSegments()
    {
        var dir = CreateTempDir();
        var sibling = Path.Combine(Path.GetTempPath(), $"sharpfm-sibling-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(sibling);
            var repo = new ClipRepository(dir);

            await repo.SaveClipsAsync([new("Evil", "Mac-XMSS", "<x/>")
                { FolderPath = new[] { "..", Path.GetFileName(sibling) } }]);

            Assert.False(File.Exists(Path.Combine(sibling, "Evil.Mac-XMSS")));
            // ".." is stripped; any remaining safe segments stay under dir.
            var written = Directory.EnumerateFiles(dir, "Evil.Mac-XMSS", SearchOption.AllDirectories).Single();
            Assert.StartsWith(dir, written);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            if (Directory.Exists(sibling)) Directory.Delete(sibling, true);
        }
    }
}
