using Microsoft.EntityFrameworkCore;
using System;


namespace SharpFM.App.Models;

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
