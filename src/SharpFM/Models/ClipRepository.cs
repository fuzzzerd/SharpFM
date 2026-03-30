using System;
using System.Collections.Generic;
using System.IO;

namespace SharpFM.Models;

/// <summary>
/// Clip File Repository.
/// </summary>
public class ClipRepository
{
    /// <summary>
    /// Clips stored in the specified folder.
    /// </summary>
    public ICollection<Clip> Clips { get; init; }

    /// <summary>
    /// Database path.
    /// </summary>
    public string ClipPath { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public ClipRepository(string path)
    {
        // ensure the directory exists
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        ClipPath = path;

        // init clips to empty
        Clips = [];
    }

    /// <summary>
    /// Load clips from the path specified by <see cref="ClipPath"/>.
    /// </summary>
    public void LoadClips()
    {
        foreach (var clipFile in Directory.EnumerateFiles(ClipPath))
        {
            var fi = new FileInfo(clipFile);

            var clip = new Clip
            {
                ClipName = fi.Name.Replace(fi.Extension, string.Empty),
                ClipType = fi.Extension.Replace(".", string.Empty),
                ClipXml = File.ReadAllText(clipFile)
            };

            Clips.Add(clip);
        }
    }

    /// <summary>
    /// Write all clips to their associated clip type files in the path specified by <see cref="ClipPath"/>.
    /// </summary>
    public void SaveChanges()
    {
        foreach (var clip in Clips)
        {
            var clipPath = Path.Combine(ClipPath, $"{clip.ClipName}.{clip.ClipType}");

            File.WriteAllText(clipPath, clip.ClipXml);
        }
    }
}
