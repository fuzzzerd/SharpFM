using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SharpFM.Diagnostics;

internal static class ClipboardSnapshotExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Write alongside the destination, then publish only a completed ZIP.</summary>
    public static async Task SaveAsync(ClipboardSnapshot snapshot, string destination)
    {
        var fullPath = Path.GetFullPath(destination);
        var temporary = Path.Combine(Path.GetDirectoryName(fullPath)!, $".sharpfm-capture-{Guid.NewGuid():N}.tmp");
        try
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                BufferSize = 81920,
                Options = FileOptions.Asynchronous
            };
            if (!OperatingSystem.IsWindows())
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            await using (var output = new FileStream(temporary, options))
            {
                using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var entry in snapshot.Entries)
                    {
                        if (entry.Data is null || entry.PayloadFile is null) continue;
                        var payload = archive.CreateEntry(entry.PayloadFile, CompressionLevel.Fastest);
                        await using var stream = payload.Open();
                        await stream.WriteAsync(entry.Data);
                    }
                    var manifest = archive.CreateEntry("manifest.json", CompressionLevel.Fastest);
                    await using var manifestStream = manifest.Open();
                    await JsonSerializer.SerializeAsync(manifestStream, snapshot, JsonOptions);
                }
                await output.FlushAsync();
            }
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
