using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using SharpFM.Model;

namespace SharpFM.Models;

/// <summary>
/// File-system-based clip repository. Stores each clip as a file in a directory.
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

        foreach (var clipFile in Directory.EnumerateFiles(ClipPath))
        {
            try
            {
                var fi = new FileInfo(clipFile);

                clips.Add(new ClipData(
                    Name: fi.Name.Replace(fi.Extension, string.Empty),
                    ClipType: fi.Extension.Replace(".", string.Empty),
                    Xml: File.ReadAllText(clipFile)));
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
        foreach (var clip in clips)
        {
            var clipPath = Path.Combine(ClipPath, $"{clip.Name}.{clip.ClipType}");
            File.WriteAllText(clipPath, clip.Xml);
        }

        // Remove files for clips that no longer exist
        var activeNames = new HashSet<string>(
            clips.Select(c => $"{c.Name}.{c.ClipType}"),
            StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(ClipPath))
        {
            if (!activeNames.Contains(Path.GetFileName(file)))
            {
                File.Delete(file);
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
}
