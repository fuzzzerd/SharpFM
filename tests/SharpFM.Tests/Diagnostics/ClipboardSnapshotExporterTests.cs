using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia.Input;
using SharpFM.Diagnostics;
using Xunit;

namespace SharpFM.Tests.Diagnostics;

public class ClipboardSnapshotExporterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"sharpfm-capture-test-{Guid.NewGuid():N}");

    public ClipboardSnapshotExporterTests() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task ArchiveContainsExactPayloadsAndVersionedManifest_WithoutRereadingClipboard()
    {
        var format = DataFormat.CreateBytesPlatformFormat("../unsafe/identifier");
        byte[] original = [0, 255, 10, 13, 60, 0, 42];
        var transfer = new ClipboardTestTransfer(new ClipboardTestItem([format, DataFormat.Text],
            f => Task.FromResult<object?>(f == format ? original : "伴随 text\r\n")));
        var opens = 0;
        var capture = new ClipboardSnapshotCapture(() =>
        {
            Assert.Equal(1, ++opens);
            return Task.FromResult<IAsyncDataTransfer?>(transfer);
        });
        var snapshot = await capture.CaptureAsync(ClipboardSnapshotCaptureTests.EnvironmentInfo, "2025", "Script steps");
        original[0] = 99; // Live/source data changes after capture must not change export.
        var destination = Path.Combine(_directory, "snapshot.zip");
        await ClipboardSnapshotExporter.SaveAsync(snapshot, destination);

        Assert.Equal(1, opens);
        using var archive = ZipFile.OpenRead(destination);
        Assert.Equal(new[] { "payloads/0000.bin", "payloads/0001.txt", "manifest.json" }, archive.Entries.Select(e => e.FullName));
        var bytes = await ReadBytes(archive.GetEntry("payloads/0000.bin")!);
        Assert.Equal(new byte[] { 0, 255, 10, 13, 60, 0, 42 }, bytes);
        Assert.Equal(Encoding.UTF8.GetBytes("伴随 text\r\n"), await ReadBytes(archive.GetEntry("payloads/0001.txt")!));
        using var document = JsonDocument.Parse(await ReadBytes(archive.GetEntry("manifest.json")!));
        var manifest = document.RootElement;
        Assert.Equal(1, manifest.GetProperty("schemaVersion").GetInt32());
        Assert.False(manifest.GetProperty("isPartial").GetBoolean());
        Assert.Equal("2025", manifest.GetProperty("fileMakerVersion").GetString());
        Assert.Equal("Script steps", manifest.GetProperty("description").GetString());
        Assert.Equal("test+build", manifest.GetProperty("environment").GetProperty("sharpFMVersion").GetString());
        Assert.Equal(snapshot.CapturedAtUtc, manifest.GetProperty("capturedAtUtc").GetDateTimeOffset());
        Assert.Contains("Avalonia", manifest.GetProperty("captureBoundary").GetString()!);
        Assert.Equal(ClipboardPayloadInterpreter.MaxProbeBytes, manifest.GetProperty("limits").GetProperty("maxXmlProbeBytes").GetInt32());
        var entries = manifest.GetProperty("entries");
        Assert.Equal(2, entries.GetArrayLength());
        Assert.False(entries[0].TryGetProperty("data", out _));
        Assert.Equal("Captured", entries[0].GetProperty("outcome").GetString());
        Assert.Equal(bytes.Length, entries[0].GetProperty("byteLength").GetInt32());
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), entries[0].GetProperty("sha256").GetString());
        Assert.Equal("../unsafe/identifier", entries[0].GetProperty("format").GetProperty("identifier").GetString());
        Assert.Equal("payloads/0000.bin", entries[0].GetProperty("payloadFile").GetString());
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task PartialCapture_ExportsSuccessesAndErrorsWithoutInventingPayloads()
    {
        var good = DataFormat.CreateBytesPlatformFormat("Mac-XMSS");
        var bad = DataFormat.CreateBytesPlatformFormat("Mac-XMunknown");
        var transfer = new ClipboardTestTransfer(new ClipboardTestItem([good, bad],
            f => f == good ? Task.FromResult<object?>(new byte[] { 1 }) : throw new IOException("do not export this secret")));
        var snapshot = await ClipboardSnapshotCaptureTests.Capture(transfer);
        var path = Path.Combine(_directory, "partial.zip");
        await ClipboardSnapshotExporter.SaveAsync(snapshot, path);

        using var archive = ZipFile.OpenRead(path);
        Assert.Equal(2, archive.Entries.Count);
        var json = await ReadBytes(archive.GetEntry("manifest.json")!);
        Assert.DoesNotContain("do not export this secret", Encoding.UTF8.GetString(json));
        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.GetProperty("isPartial").GetBoolean());
        var failed = document.RootElement.GetProperty("entries")[1];
        Assert.Equal("Failed", failed.GetProperty("outcome").GetString());
        Assert.Equal(JsonValueKind.Null, failed.GetProperty("payloadFile").ValueKind);
    }

    [Fact]
    public async Task FailedSaveLeavesNoTemporaryArchive_AndSameSnapshotCanBeRetried()
    {
        var snapshot = await Snapshot([1, 2, 3]);
        var occupied = Path.Combine(_directory, "occupied");
        Directory.CreateDirectory(occupied);
        var marker = Path.Combine(occupied, "existing.txt");
        await File.WriteAllTextAsync(marker, "keep me");

        await Assert.ThrowsAnyAsync<IOException>(() => ClipboardSnapshotExporter.SaveAsync(snapshot, occupied));
        Assert.Empty(Directory.GetFiles(_directory));
        Assert.Equal("keep me", await File.ReadAllTextAsync(marker));

        var retry = Path.Combine(_directory, "retry.zip");
        await ClipboardSnapshotExporter.SaveAsync(snapshot, retry);
        using var archive = ZipFile.OpenRead(retry);
        Assert.Equal(new byte[] { 1, 2, 3 }, await ReadBytes(archive.GetEntry("payloads/0000.bin")!));
    }

    [Fact]
    public async Task CompletedArchive_ReplacesExistingDestination()
    {
        var path = Path.Combine(_directory, "snapshot.zip");
        await File.WriteAllTextAsync(path, "previous file");
        await ClipboardSnapshotExporter.SaveAsync(await Snapshot([7]), path);
        using var archive = ZipFile.OpenRead(path);
        Assert.Equal(new byte[] { 7 }, await ReadBytes(archive.GetEntry("payloads/0000.bin")!));
        Assert.Single(Directory.GetFiles(_directory));
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
    }

    [Fact]
    public async Task PayloadAboveProbeLimit_IsExportedInFull()
    {
        var bytes = new byte[ClipboardPayloadInterpreter.MaxProbeBytes + 5];
        Random.Shared.NextBytes(bytes);
        var snapshot = await Snapshot(bytes);
        Assert.All(snapshot.Entries[0].Interpretation!.XmlHypotheses, p => Assert.Equal("Skipped", p.Utf8Status));
        var path = Path.Combine(_directory, "large.zip");
        await ClipboardSnapshotExporter.SaveAsync(snapshot, path);
        using var archive = ZipFile.OpenRead(path);
        Assert.Equal(bytes, await ReadBytes(archive.GetEntry("payloads/0000.bin")!));
    }

    [Fact]
    public async Task FailureOnlySnapshot_CanStillBeShared()
    {
        var snapshot = await new ClipboardSnapshotCapture(() => Task.FromResult<IAsyncDataTransfer?>(null))
            .CaptureAsync(ClipboardSnapshotCaptureTests.EnvironmentInfo);
        var path = Path.Combine(_directory, "failure.zip");
        await ClipboardSnapshotExporter.SaveAsync(snapshot, path);
        using var archive = ZipFile.OpenRead(path);
        Assert.Equal("manifest.json", Assert.Single(archive.Entries).FullName);
    }

    private static Task<ClipboardSnapshot> Snapshot(byte[] bytes)
    {
        var format = DataFormat.CreateBytesPlatformFormat("Mac-XMSS");
        return ClipboardSnapshotCaptureTests.Capture(new ClipboardTestTransfer(
            new ClipboardTestItem([format], _ => Task.FromResult<object?>(bytes))));
    }

    private static async Task<byte[]> ReadBytes(ZipArchiveEntry entry)
    {
        await using var stream = entry.Open();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
