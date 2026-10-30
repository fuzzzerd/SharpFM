using System.Security.Cryptography;
using System.Text;
using Avalonia.Input;
using SharpFM.Diagnostics;
using Xunit;

namespace SharpFM.Tests.Diagnostics;

public class ClipboardSnapshotCaptureTests
{
    internal static readonly ClipboardCaptureEnvironment EnvironmentInfo = new("test+build", "test OS", "test arch", "test Avalonia");

    [Fact]
    public async Task CapturesEveryItemAndFormat_UsingOriginalDescriptors_AndOneTransfer()
    {
        var format = DataFormat.CreateBytesPlatformFormat("com.example.filemaker-unknown");
        byte[] original = [0x00, 0xFF, 0x0D, 0x0A, 0x3C];
        var expected = original.ToArray();
        var reads = new List<DataFormat>();
        var transfer = new ClipboardTestTransfer(
            new ClipboardTestItem([format, DataFormat.Text], f =>
            {
                reads.Add(f);
                return Task.FromResult<object?>(f == format ? original : "Hello\r\n世界");
            }),
            new ClipboardTestItem([format], f =>
            {
                Assert.Same(format, f);
                reads.Add(f);
                return Task.FromResult<object?>(new byte[] { 1, 2 });
            }))
        {
            OnDispose = () => Array.Fill(original, (byte)0)
        };
        var opens = 0;
        var capture = new ClipboardSnapshotCapture(() =>
        {
            opens++;
            return Task.FromResult<IAsyncDataTransfer?>(transfer);
        });
        var snapshot = await capture.CaptureAsync(EnvironmentInfo, " 2025 ", " Script steps ");

        Assert.Equal(1, opens);
        Assert.True(transfer.Disposed);
        Assert.Equal(3, reads.Count);
        Assert.Same(format, reads[0]);
        Assert.Same(DataFormat.Text, reads[1]);
        Assert.Equal(2, snapshot.Formats.Count);
        Assert.Equal(3, snapshot.Entries.Count);
        Assert.False(snapshot.IsPartial);
        Assert.Equal(EnvironmentInfo, snapshot.Environment);
        Assert.Equal("2025", snapshot.FileMakerVersion);
        Assert.Equal("Script steps", snapshot.Description);
        Assert.Equal(1, snapshot.SchemaVersion);
        Assert.Equal(expected, snapshot.Entries[0].Data);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(expected)), snapshot.Entries[0].Sha256);
        Assert.Equal("System.Byte[]", snapshot.Entries[0].RuntimeType);
        Assert.Equal("System.Byte[]", snapshot.Entries[0].Format.ExposedRepresentation);
        Assert.Equal(Encoding.UTF8.GetBytes("Hello\r\n世界"), snapshot.Entries[1].Data);
        Assert.Contains("not wire bytes", snapshot.Entries[1].PayloadRepresentation!);
        Assert.Equal(0, snapshot.Entries[0].ItemIndex);
        Assert.Equal(1, snapshot.Entries[2].ItemIndex);
        Assert.Equal(3, snapshot.Entries.Select(e => e.PayloadFile).Distinct().Count());
    }

    [Fact]
    public async Task FailedNullUnsupportedAndInvalidText_DoNotDiscardSuccessfulEntries()
    {
        var formats = Enumerable.Range(0, 5).Select(i => DataFormat.CreateBytesPlatformFormat($"format-{i}")).ToArray();
        var transfer = new ClipboardTestTransfer(new ClipboardTestItem(formats, f =>
        {
            var i = Array.IndexOf(formats, f);
            return i switch
            {
                0 => throw new IOException("secret password in /home/private/path"),
                1 => Task.FromResult<object?>(null),
                2 => Task.FromResult<object?>(new object()),
                3 => Task.FromResult<object?>("\uD800"), // Invalid surrogate; do not silently replace it.
                _ => Task.FromResult<object?>(new byte[] { 0x42 })
            };
        }));
        var snapshot = await Capture(transfer);

        Assert.True(transfer.Disposed);
        Assert.True(snapshot.IsPartial);
        Assert.Equal(new[] { ClipboardCaptureOutcome.Failed, ClipboardCaptureOutcome.Null,
            ClipboardCaptureOutcome.Unsupported, ClipboardCaptureOutcome.Failed, ClipboardCaptureOutcome.Captured },
            snapshot.Entries.Select(e => e.Outcome));
        Assert.Equal("System.Object", snapshot.Entries[2].RuntimeType);
        Assert.DoesNotContain("secret", snapshot.Entries[0].Error!);
        Assert.DoesNotContain("/home", snapshot.Entries[0].Error!);
        Assert.Contains("System.IO.IOException", snapshot.Entries[0].Error!);
        Assert.Null(snapshot.Entries[3].Data);
        Assert.Equal(new byte[] { 0x42 }, snapshot.Entries[4].Data);
    }

    [Fact]
    public async Task NativeStringDescriptorAndEmptyPayloads_AreCapturedWithoutByteFormatRequests()
    {
        var textFormat = DataFormat.CreateStringPlatformFormat("com.example.filemaker.text");
        var bytesFormat = DataFormat.CreateBytesPlatformFormat("empty-bytes");
        var transfer = new ClipboardTestTransfer(new ClipboardTestItem([textFormat, bytesFormat], f =>
        {
            Assert.True(ReferenceEquals(f, textFormat) || ReferenceEquals(f, bytesFormat));
            return Task.FromResult<object?>(f == textFormat ? "" : Array.Empty<byte>());
        }));
        var snapshot = await Capture(transfer, budget: 0);

        Assert.False(snapshot.IsPartial);
        Assert.Equal("System.String", snapshot.Entries[0].Format.ExposedRepresentation);
        Assert.Equal("System.String", snapshot.Entries[0].RuntimeType);
        Assert.All(snapshot.Entries, e =>
        {
            Assert.Equal(ClipboardCaptureOutcome.Captured, e.Outcome);
            Assert.Equal(0, e.ByteLength);
            Assert.NotNull(e.Data);
            Assert.Empty(e.Data!);
            Assert.NotNull(e.PayloadFile);
        });
    }

    [Fact]
    public async Task ItemDiscoveryFailure_DoesNotPreventReadingLaterItems()
    {
        var format = DataFormat.CreateBytesPlatformFormat("Mac-XMSS");
        var transfer = new ClipboardTestTransfer(
            new BrokenItem(), new ClipboardTestItem([format], _ => Task.FromResult<object?>(new byte[] { 1 })))
        {
            DiscoverFormats = () => [format]
        };
        var snapshot = await Capture(transfer);
        Assert.True(snapshot.IsPartial);
        Assert.Equal(1, Assert.Single(snapshot.Entries).ItemIndex);
        Assert.Contains("Item 0 format discovery", Assert.Single(snapshot.Issues));
        Assert.True(transfer.Disposed);
    }

    [Fact]
    public async Task RetentionBudget_ReportsOmissionWithoutTruncation_AndContinues()
    {
        var formats = Enumerable.Range(0, 4).Select(i => DataFormat.CreateBytesPlatformFormat($"format-{i}")).ToArray();
        byte[][] values = [new byte[6], new byte[3], new byte[3], new byte[2]];
        var transfer = new ClipboardTestTransfer(new ClipboardTestItem(formats,
            f => Task.FromResult<object?>(values[Array.IndexOf(formats, f)])));
        var snapshot = await Capture(transfer, budget: 5);

        Assert.True(snapshot.IsPartial);
        Assert.Equal(5, snapshot.Limits.MaxRetainedPayloadBytes);
        Assert.Equal(ClipboardCaptureOutcome.ResourceLimit, snapshot.Entries[0].Outcome);
        Assert.Equal(6, snapshot.Entries[0].ByteLength);
        Assert.Null(snapshot.Entries[0].Data);
        Assert.Null(snapshot.Entries[0].PayloadFile);
        Assert.Equal(values[1], snapshot.Entries[1].Data);
        Assert.Equal(ClipboardCaptureOutcome.ResourceLimit, snapshot.Entries[2].Outcome);
        Assert.Equal(values[3], snapshot.Entries[3].Data);
    }

    [Fact]
    public async Task DiscoveryFailure_StillCapturesItemsAndTheirIdentifiers()
    {
        var format = DataFormat.CreateBytesPlatformFormat("unknown-format");
        var transfer = new ClipboardTestTransfer(new ClipboardTestItem([format], _ => Task.FromResult<object?>(new byte[] { 1 })))
        {
            DiscoverFormats = () => throw new InvalidOperationException("private data")
        };
        var snapshot = await Capture(transfer);

        Assert.True(snapshot.IsPartial);
        Assert.Single(snapshot.Formats);
        Assert.Equal(ClipboardCaptureOutcome.Captured, Assert.Single(snapshot.Entries).Outcome);
        Assert.Single(snapshot.Issues);
        Assert.DoesNotContain("private data", snapshot.Issues[0]);
    }

    [Fact]
    public async Task AdvertisedFormatWithoutItem_IsReportedAsPartial()
    {
        var transfer = new ClipboardTestTransfer
        {
            DiscoverFormats = () => [DataFormat.CreateBytesPlatformFormat("missing-item")]
        };
        var snapshot = await Capture(transfer);
        Assert.Single(snapshot.Formats);
        Assert.Empty(snapshot.Entries);
        Assert.True(snapshot.IsPartial);
        Assert.Contains(snapshot.Issues, i => i.Contains("no accessible item"));
    }

    [Fact]
    public async Task DisposalFailure_PreservesCapturedPayload()
    {
        var format = DataFormat.CreateBytesPlatformFormat("Mac-XMSS");
        var transfer = new ClipboardTestTransfer(new ClipboardTestItem([format], _ => Task.FromResult<object?>(new byte[] { 1 })))
        {
            OnDispose = () => throw new IOException("private path")
        };
        var snapshot = await Capture(transfer);
        Assert.True(snapshot.IsPartial);
        Assert.Equal(new byte[] { 1 }, Assert.Single(snapshot.Entries).Data);
        Assert.Single(snapshot.Issues);
    }

    [Fact]
    public async Task UnavailableOrThrowingClipboard_ProducesExportableFailureSnapshot()
    {
        var missing = await new ClipboardSnapshotCapture(() => Task.FromResult<IAsyncDataTransfer?>(null))
            .CaptureAsync(EnvironmentInfo);
        Assert.True(missing.IsPartial);
        Assert.Empty(missing.Entries);
        Assert.Single(missing.Issues);

        var failed = await new ClipboardSnapshotCapture(() => throw new IOException("sensitive message"))
            .CaptureAsync(EnvironmentInfo);
        Assert.True(failed.IsPartial);
        Assert.Empty(failed.Entries);
        Assert.Contains("IOException", Assert.Single(failed.Issues));
        Assert.DoesNotContain("sensitive", failed.Issues[0]);
    }

    private sealed class BrokenItem : IAsyncDataTransferItem
    {
        public IReadOnlyList<DataFormat> Formats => throw new IOException("sensitive discovery error");
        public Task<object?> TryGetRawAsync(DataFormat format) => throw new InvalidOperationException("Must not read an undiscovered format.");
    }

    internal static Task<ClipboardSnapshot> Capture(ClipboardTestTransfer transfer, long budget = 128L * 1024 * 1024) =>
        new ClipboardSnapshotCapture(() => Task.FromResult<IAsyncDataTransfer?>(transfer), budget).CaptureAsync(EnvironmentInfo);
}
