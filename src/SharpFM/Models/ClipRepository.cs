using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using SharpFM.Model;

namespace SharpFM.Models;

/// <summary>
/// File-system-based clip repository. Stores each clip as a file in a directory
/// tree. Subdirectories map one-to-one to <see cref="ClipData.FolderPath"/>
/// segments.
/// </summary>
public class ClipRepository : IClipRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public string ProviderName => "Local Files";

    public string CurrentLocation => ClipPath;

    public bool SupportsLocationPicker => false;

    /// <summary>
    /// Directory path where clip files are stored.
    /// </summary>
    public string ClipPath { get; }

    public ClipRepository(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        ClipPath = path;
    }

    public Task<IReadOnlyList<ClipData>> LoadClipsAsync()
    {
        var clips = new List<ClipData>();
        var root = new DirectoryInfo(ClipPath);

        foreach (var clipFile in Directory.EnumerateFiles(ClipPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                var fi = new FileInfo(clipFile);
                if (string.IsNullOrEmpty(fi.Extension)) continue;

                var folderPath = GetRelativeFolderSegments(root.FullName, fi.Directory!.FullName);

                clips.Add(new ClipData(
                    Name: Path.GetFileNameWithoutExtension(fi.Name),
                    ClipType: fi.Extension.TrimStart('.'),
                    Xml: File.ReadAllText(clipFile))
                {
                    FolderPath = folderPath,
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load clip file: {File}", clipFile);
            }
        }

        return Task.FromResult<IReadOnlyList<ClipData>>(clips);
    }

    public Task SaveClipsAsync(IReadOnlyList<ClipData> clips)
    {
        var activeRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var clip in clips)
        {
            var safeSegments = SanitizeFolderPath(clip.FolderPath);
            var targetDir = safeSegments.Count == 0
                ? ClipPath
                : Path.Combine(new[] { ClipPath }.Concat(safeSegments).ToArray());

            Directory.CreateDirectory(targetDir);

            var fileName = $"{clip.Name}.{clip.ClipType}";
            var clipPath = Path.Combine(targetDir, fileName);
            File.WriteAllText(clipPath, clip.Xml);

            var relative = Path.GetRelativePath(ClipPath, clipPath);
            activeRelativePaths.Add(relative);
        }

        // Remove files for clips that no longer exist (anywhere in the tree).
        foreach (var file in Directory.EnumerateFiles(ClipPath, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(ClipPath, file);
            if (!activeRelativePaths.Contains(relative))
            {
                File.Delete(file);
            }
        }

        // Clean up any now-empty subdirectories (bottom-up).
        foreach (var dir in Directory.EnumerateDirectories(ClipPath, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                try { Directory.Delete(dir); }
                catch (IOException) { /* best-effort */ }
            }
        }

        return Task.CompletedTask;
    }

    public Task<string?> PickLocationAsync()
    {
        // File-based repo doesn't handle its own location picking.
        // The host manages folder selection and creates a new ClipRepository.
        return Task.FromResult<string?>(null);
    }

    private static IReadOnlyList<string> GetRelativeFolderSegments(string root, string directory)
    {
        var rel = Path.GetRelativePath(root, directory);
        if (string.IsNullOrEmpty(rel) || rel == ".") return [];
        return rel.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
    }

    // Reject traversal/rooted segments; repositories are logical stores and
    // must not escape their root no matter what a misbehaving provider sends.
    private static IReadOnlyList<string> SanitizeFolderPath(IReadOnlyList<string> segments)
    {
        if (segments is null || segments.Count == 0) return [];
        var safe = new List<string>(segments.Count);
        foreach (var raw in segments)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (raw == "." || raw == "..") continue;
            if (raw.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) continue;
            safe.Add(raw);
        }
        return safe;
    }
}
