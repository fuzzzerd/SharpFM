using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

public class ClipDbContext : DbContext
{
    public DbSet<Clip> Clips { get; set; }

    public string DbPath { get; }

    public ClipDbContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "blogging.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}

public class Clip
{
    public int ClipId { get; set; }
    public string ClipName { get; set; } = string.Empty;
    public string ClipType { get; set; } = string.Empty;
    public string ClipXml { get; set; } = string.Empty;
}
