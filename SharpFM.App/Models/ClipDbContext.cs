using Microsoft.EntityFrameworkCore;
using System;

/// <summary>
/// Clip Db Context
/// </summary>
public class ClipDbContext : DbContext
{
    /// <summary>
    /// Clips stored in the Db.
    /// </summary>
    public DbSet<Clip> Clips => Set<Clip>();

    /// <summary>
    /// Database path.
    /// </summary>
    public string DbPath { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public ClipDbContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "sharpFM.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}

/// <summary>
/// Clip Data Model
/// </summary>
public class Clip
{
    /// <summary>
    /// Database Id
    /// </summary>
    public int ClipId { get; set; }

    /// <summary>
    /// Display name for clip may match Name inside the xml data or may not.
    /// </summary>
    public string ClipName { get; set; } = string.Empty;

    /// <summary>
    /// The data format to use when putting the data back on the clipboard for FileMaker.
    /// </summary>
    public string ClipType { get; set; } = string.Empty;

    /// <summary>
    /// Raw xml data from the clip.
    /// </summary>
    public string ClipXml { get; set; } = string.Empty;
}
