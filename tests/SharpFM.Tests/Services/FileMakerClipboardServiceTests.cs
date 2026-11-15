using System.Buffers.Binary;
using System.Text;
using System.Xml.Linq;
using Avalonia.Input;
using SharpFM.Model;
using SharpFM.Services;
using SharpFM.Tests.Diagnostics;
using Xunit;

namespace SharpFM.Tests.Services;

public class FileMakerClipboardServiceTests
{
    public static TheoryData<string, string, string> Captures => new()
    {
        { "whole-script", "Mac-XMSC", "dyn.ah62d4rv4gk8zuxnxkq" },
        { "script-steps", "Mac-XMSS", "dyn.ah62d4rv4gk8zuxnxnq" },
        { "full-table", "Mac-XMTB", "dyn.ah62d4rv4gk8zuxnykk" },
        { "table-fields", "Mac-XMFD", "dyn.ah62d4rv4gk8zuxngku" },
        { "layout-element", "Mac-XML2", "dyn.ah62d4rv4gk8zuxnqgk" },
        { "custom-function", "Mac-XMFN", "dyn.ah62d4rv4gk8zuxngm2" },
        { "value-list", "Mac-XMVL", "dyn.ah62d4rv4gk8zuxn0mu" }
    };

    [Theory]
    [MemberData(nameof(Captures))]
    public async Task MacCapture_MapsIncomingStringAndOutgoingFormat(string file, string logical, string native)
    {
        var xml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "FileMakerClipboard", file + ".xml"));
        var format = DataFormat.CreateStringPlatformFormat(native);
        var transfer = new ClipboardTestTransfer(new ClipboardTestItem([format], requested =>
        {
            Assert.Same(format, requested);
            return Task.FromResult<object?>(xml);
        }));
        IAsyncDataTransfer? output = null;
        var service = new ClipboardService(() => Task.FromResult<IAsyncDataTransfer?>(transfer),
            data => { output = data; return Task.CompletedTask; }, isMacOS: true);

        var clip = Assert.Single(await service.ReadFileMakerAsync());
        Assert.Equal(logical, clip.FormatId);
        Assert.True(XNode.DeepEquals(XDocument.Parse(xml), XDocument.Parse(clip.Xml)));
        Assert.True(transfer.Disposed);
        clip = clip.WithXml("<fmxmlsnippet>édité 日本語 😀</fmxmlsnippet>");
        await service.WriteFileMakerAsync(clip);
        var outgoing = Assert.Single(output!.Items[0].Formats);
        Assert.Equal(native, outgoing.Identifier);
        Assert.Equal(clip.Xml, Assert.IsType<string>(await output.Items[0].TryGetRawAsync(outgoing)));
    }

    [Theory]
    [InlineData("Mac-XMSS")]
    [InlineData("Mac-XMZZ")]
    public async Task Windows_PreservesPrefixedBytesAndLogicalId(string logical)
    {
        var clip = Clip.FromXml("test", logical, "<fmxmlsnippet>café 日本語 😀</fmxmlsnippet>");
        var format = DataFormat.CreateBytesPlatformFormat(logical);
        var transfer = new ClipboardTestTransfer(new ClipboardTestItem([format], _ => Task.FromResult<object?>(clip.WireBytes)));
        IAsyncDataTransfer? output = null;
        var service = new ClipboardService(() => Task.FromResult<IAsyncDataTransfer?>(transfer),
            data => { output = data; return Task.CompletedTask; }, isMacOS: false);

        var imported = Assert.Single(await service.ReadFileMakerAsync());
        Assert.Equal(logical, imported.FormatId);
        Assert.Equal(clip.Xml, imported.Xml);
        await service.WriteFileMakerAsync(clip);
        var outgoing = Assert.Single(output!.Items[0].Formats);
        Assert.Equal(logical, outgoing.Identifier);
        var bytes = Assert.IsType<byte[]>(await output.Items[0].TryGetRawAsync(outgoing));
        Assert.Equal(Encoding.UTF8.GetByteCount(clip.Xml), (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        Assert.Equal(clip.Xml, Encoding.UTF8.GetString(bytes, 4, bytes.Length - 4));
    }

    [Fact]
    public async Task CopyWithoutPriorCapture_SelectsFormatFromRuntime()
    {
        var clip = Clip.FromXml("test", "Mac-XMSS", "<fmxmlsnippet/>");
        foreach (var mac in new[] { false, true })
        {
            IAsyncDataTransfer? output = null;
            var service = new ClipboardService(() => throw new Exception("must not read"),
                data => { output = data; return Task.CompletedTask; }, mac);
            await service.WriteFileMakerAsync(clip);
            Assert.Equal(mac ? "dyn.ah62d4rv4gk8zuxnxnq" : "Mac-XMSS", output!.Items[0].Formats[0].Identifier);
        }
    }

    [Fact]
    public async Task UnrelatedOrUnavailableClipboard_IsIgnored()
    {
        foreach (var transfer in new IAsyncDataTransfer?[] { null,
            new ClipboardTestTransfer(new ClipboardTestItem([DataFormat.Text], _ => Task.FromResult<object?>("plain text"))) })
        {
            var service = new ClipboardService(() => Task.FromResult(transfer), _ => throw new Exception("must not write"), false);
            Assert.Empty(await service.ReadFileMakerAsync());
        }
    }

    [Fact]
    public async Task UnknownMacOutputType_FailsInsteadOfWritingAWrongFormat()
    {
        var service = new ClipboardService(() => throw new Exception("must not read"),
            _ => throw new Exception("must not write"), true);
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            service.WriteFileMakerAsync(Clip.FromXml("test", "Mac-XMZZ", "<fmxmlsnippet/>")));
    }
}
