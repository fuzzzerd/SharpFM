using System.Text;
using Avalonia.Input;
using SharpFM.Diagnostics;
using Xunit;

namespace SharpFM.Tests.Diagnostics;

public class ClipboardSnapshotSummaryTests
{
    [Fact]
    public async Task SummaryShowsDiscoveryTypesAndIndependentHypotheses()
    {
        var format = DataFormat.CreateBytesPlatformFormat("unknown-filemaker-format");
        var bytes = Encoding.UTF8.GetBytes("<fmxmlsnippet type=\"FMObjectList\" />");
        var snapshot = await ClipboardSnapshotCaptureTests.Capture(new ClipboardTestTransfer(
            new ClipboardTestItem([format], _ => Task.FromResult<object?>(bytes))));

        var summary = ClipboardSnapshotSummary.Describe(snapshot);
        Assert.Contains("unknown-filemaker-format", summary);
        Assert.Contains("1/1", summary);
        Assert.Contains("not a native pasteboard dump", summary);

        var detail = ClipboardSnapshotSummary.Describe(snapshot.Entries[0]);
        Assert.Contains("System.Byte[]", detail);
        Assert.Contains("Offset 0: UTF-8 Valid; XML Valid", detail);
        Assert.Contains("Offset 4:", detail);
        Assert.Contains("not detected platform rules", detail);
        Assert.Contains("FMObjectList", detail);
        Assert.DoesNotContain("<fmxmlsnippet", detail);
    }

    [Fact]
    public async Task PartialCaptureSummaryIncludesFailureAndDiscoveredFormats()
    {
        var format = DataFormat.CreateBytesPlatformFormat("Mac-XMSS");
        var snapshot = await ClipboardSnapshotCaptureTests.Capture(new ClipboardTestTransfer(
            new ClipboardTestItem([format], _ => throw new IOException("sensitive text"))));
        Assert.Contains("partial capture", ClipboardSnapshotSummary.Describe(snapshot));
        var detail = ClipboardSnapshotSummary.Describe(snapshot.Entries[0]);
        Assert.Contains("Failed", detail);
        Assert.Contains("IOException", detail);
        Assert.DoesNotContain("sensitive text", detail);
    }
}
