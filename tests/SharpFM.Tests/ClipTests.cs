using System.Text;
using SharpFM.Model;
using SharpFM.Model.ClipTypes;
using SharpFM.Model.Parsing;

namespace SharpFM.Tests;

public class ClipTests : IDisposable
{
    public ClipTests()
    {
        ClipTypeRegistry.Reset();
    }

    public void Dispose()
    {
        ClipTypeRegistry.Reset();
    }

    [Fact]
    public void FromXml_UnknownFormat_FallsBackToOpaqueParse()
    {
        var clip = Clip.FromXml("Untitled", "Mac-XMUNKNOWN", "<root/>");

        Assert.Equal("Untitled", clip.Name);
        Assert.Equal("Mac-XMUNKNOWN", clip.FormatId);
        Assert.IsType<ParseSuccess>(clip.Parsed);
    }

    [Fact]
    public void FromXml_PrettyPrintsCanonicalForm()
    {
        var clip = Clip.FromXml("X", "Mac-XMUNKNOWN", "<root><child/></root>");

        Assert.Contains("\n", clip.Xml);
        Assert.Contains("<child", clip.Xml);
    }

    [Fact]
    public void FromXml_MalformedXml_DoesNotThrow_ReportsFailure()
    {
        var clip = Clip.FromXml("X", "Mac-XMUNKNOWN", "<oops>");

        Assert.IsType<ParseFailure>(clip.Parsed);
        Assert.False(clip.Parsed.Report.IsLossless);
    }

    [Fact]
    public void FromXml_NullXml_TreatedAsEmpty()
    {
        var clip = Clip.FromXml("X", "Mac-XMUNKNOWN", null!);

        Assert.IsType<ParseFailure>(clip.Parsed);
    }

    [Fact]
    public void FromWireBytes_StripsLengthPrefix()
    {
        const string xml = "<root/>";
        var payload = Encoding.UTF8.GetBytes(xml);
        var prefix = BitConverter.GetBytes(payload.Length);
        var bytes = prefix.Concat(payload).ToArray();

        var clip = Clip.FromWireBytes("X", "Mac-XMUNKNOWN", bytes);

        Assert.IsType<ParseSuccess>(clip.Parsed);
    }

    [Fact]
    public void FromWireBytes_TooShortInput_TreatedAsEmpty()
    {
        var clip = Clip.FromWireBytes("X", "Mac-XMUNKNOWN", [0x00, 0x01]);
        Assert.IsType<ParseFailure>(clip.Parsed);
    }

    [Fact]
    public void WireBytes_RoundTripsThroughLengthPrefix()
    {
        var clip = Clip.FromXml("X", "Mac-XMUNKNOWN", "<root/>");
        var bytes = clip.WireBytes;

        var prefix = BitConverter.ToInt32(bytes, 0);
        Assert.Equal(bytes.Length - 4, prefix);
        var payload = Encoding.UTF8.GetString(bytes, 4, bytes.Length - 4);
        Assert.Equal(clip.Xml, payload);
    }

    [Fact]
    public void WireBytes_IsCachedAcrossReads()
    {
        var clip = Clip.FromXml("X", "Mac-XMUNKNOWN", "<root/>");
        Assert.Same(clip.WireBytes, clip.WireBytes);
    }

    [Fact]
    public void WithXml_ProducesNewInstanceWithReparse()
    {
        var original = Clip.FromXml("X", "Mac-XMUNKNOWN", "<root/>");
        var updated = original.WithXml("<other/>");

        Assert.NotSame(original, updated);
        Assert.Contains("other", updated.Xml);
        Assert.Equal(original.Name, updated.Name);
        Assert.Equal(original.FormatId, updated.FormatId);
    }

    [Fact]
    public void Rename_ReusesParsedResult()
    {
        var original = Clip.FromXml("X", "Mac-XMUNKNOWN", "<root/>");
        var renamed = original.Rename("Y");

        Assert.Same(original.Parsed, renamed.Parsed);
        Assert.Equal("Y", renamed.Name);
        Assert.Equal(original.Xml, renamed.Xml);
    }
}
