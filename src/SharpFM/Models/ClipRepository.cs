using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NLog;
using SharpFM.Model;

namespace SharpFM.Models;

/// <summary>
/// File-system-based clip repository. Stores each clip as a file in a directory
/// tree. Subdirectories map one-to-one to <see cref="ClipData.FolderPath"/>
/// segments. Per-folder metadata (FileMaker Group id, includeInMenu,
/// groupCollapsed) is persisted in a <see cref="FolderMarkerFileName"/> sidecar
/// inside each folder; an "empty folder" is a directory that contains only the
/// sidecar.
/// </summary>
public class ClipRepository : IClipRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>Sidecar filename used to store folder metadata and to mark
    /// empty folders so the orphan sweep doesn't reclaim them.</summary>
    public const string FolderMarkerFileName = ".sharpfm-folder.json";

    private static readonly JsonSerializerOptions FolderJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private sealed record FolderMarker
    {
        public int? Id { get; init; }
        public bool IncludeInMenu { get; init; } = true;
        public bool GroupCollapsed { get; init; }
    }

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
                if (string.Equals(fi.Name, FolderMarkerFileName, StringComparison.OrdinalIgnoreCase)) continue;

                var folderPath = GetRelativeFolderSegments(root.FullName, fi.Directory!.FullName);

                clips.Add(new ClipData(
                    Name: DecodeName(Path.GetFileNameWithoutExtension(fi.Name)),
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

            var fileName = $"{EncodeName(clip.Name)}.{clip.ClipType}";
            var clipPath = Path.Combine(targetDir, fileName);
            File.WriteAllText(clipPath, clip.Xml);

            var relative = Path.GetRelativePath(ClipPath, clipPath);
            activeRelativePaths.Add(relative);
        }

        // Remove files for clips that no longer exist (anywhere in the tree).
        // Folder marker files are preserved by SaveFoldersAsync — they belong
        // to the folder, not to any individual clip.
        foreach (var file in Directory.EnumerateFiles(ClipPath, "*", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFileName(file), FolderMarkerFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var relative = Path.GetRelativePath(ClipPath, file);
            if (!activeRelativePaths.Contains(relative))
            {
                File.Delete(file);
            }
        }

        PruneEmptyDirectories();

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FolderData>> LoadFoldersAsync()
    {
        var folders = new List<FolderData>();
        var root = new DirectoryInfo(ClipPath);

        foreach (var markerPath in Directory.EnumerateFiles(ClipPath, FolderMarkerFileName, SearchOption.AllDirectories))
        {
            try
            {
                var fi = new FileInfo(markerPath);
                var path = GetRelativeFolderSegments(root.FullName, fi.Directory!.FullName);
                if (path.Count == 0) continue;

                var marker = JsonSerializer.Deserialize<FolderMarker>(File.ReadAllText(markerPath), FolderJsonOptions)
                             ?? new FolderMarker();

                folders.Add(new FolderData(path)
                {
                    Id = marker.Id,
                    IncludeInMenu = marker.IncludeInMenu,
                    GroupCollapsed = marker.GroupCollapsed,
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load folder marker: {File}", markerPath);
            }
        }

        return Task.FromResult<IReadOnlyList<FolderData>>(folders);
    }

    public Task SaveFoldersAsync(IReadOnlyList<FolderData> folders)
    {
        var activeMarkerPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders)
        {
            var safeSegments = SanitizeFolderPath(folder.Path);
            if (safeSegments.Count == 0) continue;

            var targetDir = Path.Combine(new[] { ClipPath }.Concat(safeSegments).ToArray());
            Directory.CreateDirectory(targetDir);

            var marker = new FolderMarker
            {
                Id = folder.Id,
                IncludeInMenu = folder.IncludeInMenu,
                GroupCollapsed = folder.GroupCollapsed,
            };

            var markerPath = Path.Combine(targetDir, FolderMarkerFileName);
            File.WriteAllText(markerPath, JsonSerializer.Serialize(marker, FolderJsonOptions));
            activeMarkerPaths.Add(markerPath);
        }

        // Remove orphan marker files. Empty directories are reclaimed below.
        foreach (var marker in Directory.EnumerateFiles(ClipPath, FolderMarkerFileName, SearchOption.AllDirectories))
        {
            if (!activeMarkerPaths.Contains(marker))
            {
                File.Delete(marker);
            }
        }

        PruneEmptyDirectories();

        return Task.CompletedTask;
    }

    private void PruneEmptyDirectories()
    {
        foreach (var dir in Directory.EnumerateDirectories(ClipPath, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                try { Directory.Delete(dir); }
                catch (IOException) { /* best-effort */ }
            }
        }
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
        var raw = rel.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        var decoded = new string[raw.Length];
        for (var i = 0; i < raw.Length; i++) decoded[i] = DecodeName(raw[i]);
        return decoded;
    }

    // Reject traversal segments (security) and percent-encode any character
    // the filesystem rejects so FileMaker names containing '/' or ':' survive
    // the round-trip instead of being silently dropped.
    private static IReadOnlyList<string> SanitizeFolderPath(IReadOnlyList<string> segments)
    {
        if (segments is null || segments.Count == 0) return [];
        var safe = new List<string>(segments.Count);
        foreach (var raw in segments)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (raw == "." || raw == "..") continue;
            safe.Add(EncodeName(raw));
        }
        return safe;
    }

    private static readonly char[] InvalidNameChars = Path.GetInvalidFileNameChars();

    /// <summary>
    /// Percent-encode characters the filesystem rejects (path separators,
    /// reserved chars, control codes) plus '%' itself so encoding is reversible.
    /// Output is plain ASCII and safe to embed in any cross-platform filename.
    /// </summary>
    internal static string EncodeName(string raw)
    {
        var needsEncoding = false;
        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] == '%' || Array.IndexOf(InvalidNameChars, raw[i]) >= 0)
            {
                needsEncoding = true;
                break;
            }
        }
        if (!needsEncoding) return raw;

        var sb = new System.Text.StringBuilder(raw.Length + 8);
        foreach (var ch in raw)
        {
            if (ch == '%' || Array.IndexOf(InvalidNameChars, ch) >= 0)
            {
                sb.Append('%');
                sb.Append(((int)ch).ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    /// <summary>Reverse of <see cref="EncodeName"/>. Unrecognised '%' triplets pass through unchanged.</summary>
    internal static string DecodeName(string encoded)
    {
        if (encoded.IndexOf('%') < 0) return encoded;

        var sb = new System.Text.StringBuilder(encoded.Length);
        for (var i = 0; i < encoded.Length; i++)
        {
            if (encoded[i] == '%' && i + 2 < encoded.Length
                && int.TryParse(encoded.AsSpan(i + 1, 2),
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var code))
            {
                sb.Append((char)code);
                i += 2;
            }
            else
            {
                sb.Append(encoded[i]);
            }
        }
        return sb.ToString();
    }
}
