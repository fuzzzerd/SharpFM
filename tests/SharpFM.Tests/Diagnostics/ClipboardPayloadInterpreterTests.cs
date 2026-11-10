using System.Buffers.Binary;
using System.Text;
using SharpFM.Diagnostics;
using Xunit;

namespace SharpFM.Tests.Diagnostics;

public class ClipboardPayloadInterpreterTests
{
    private const string Xml = "<fmxmlsnippet type=\"FMObjectList\"><Step /></fmxmlsnippet>";

    [Fact]
    public void PrefixedXml_IsValidOnlyAtOffsetFour_WithoutChangingBytes()
    {
        var xml = Encoding.UTF8.GetBytes(Xml);
        var payload = new byte[xml.Length + 4];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, (uint)xml.Length);
        xml.CopyTo(payload, 4);
        var original = payload.ToArray();

        var interpretation = ClipboardPayloadInterpreter.Interpret(payload);

        Assert.Equal(original, payload);
        Assert.Equal((uint)xml.Length, interpretation.LittleEndianLength);
        Assert.True(interpretation.LengthMatchesRemainingBytes);
        Assert.NotEqual("Valid", interpretation.XmlHypotheses[0].XmlStatus);
        var probe = interpretation.XmlHypotheses[1];
        Assert.Equal(4, probe.Offset);
        Assert.Equal("Valid", probe.XmlStatus);
        Assert.Equal("fmxmlsnippet", probe.RootName);
        Assert.Equal("FMObjectList", probe.SnippetType);
        Assert.Equal(Convert.ToHexString(payload.AsSpan(0, 32)), interpretation.LeadingHex);
    }

    [Fact]
    public void UnprefixedXml_IsValidAtZero_WithoutAssumingMacOrWindows()
    {
        var interpretation = ClipboardPayloadInterpreter.Interpret(Encoding.UTF8.GetBytes(Xml));
        Assert.False(interpretation.LengthMatchesRemainingBytes);
        Assert.Equal("Valid", interpretation.XmlHypotheses[0].XmlStatus);
        Assert.Equal("Invalid", interpretation.XmlHypotheses[1].XmlStatus);
    }

    [Fact]
    public void WrongHeader_IsReported_WithoutPreventingOffsetFourProbe()
    {
        var payload = new byte[Encoding.UTF8.GetByteCount(Xml) + 4];
        Encoding.UTF8.GetBytes(Xml).CopyTo(payload, 4);
        BinaryPrimitives.WriteUInt32LittleEndian(payload, uint.MaxValue);
        var result = ClipboardPayloadInterpreter.Interpret(payload);
        Assert.Equal(uint.MaxValue, result.LittleEndianLength);
        Assert.False(result.LengthMatchesRemainingBytes);
        Assert.Equal("Valid", result.XmlHypotheses[1].XmlStatus);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void ShortInputs_DoNotInventLengthHeader(int size)
    {
        var result = ClipboardPayloadInterpreter.Interpret(new byte[size]);
        Assert.Null(result.LittleEndianLength);
        Assert.Null(result.LengthMatchesRemainingBytes);
        Assert.Equal("Unavailable", result.XmlHypotheses[1].Utf8Status);
        Assert.NotEqual("Valid", result.XmlHypotheses[0].XmlStatus);
    }

    [Fact]
    public void InvalidUtf8_IsNotSilentlyReplaced()
    {
        var result = ClipboardPayloadInterpreter.Interpret([0xFF, 0xFE, 0x3C, 0x00]);
        Assert.Equal("UTF-16 LE", result.Bom);
        Assert.Equal("Invalid", result.XmlHypotheses[0].Utf8Status);
        Assert.Equal("NotAttempted", result.XmlHypotheses[0].XmlStatus);
    }

    [Theory]
    [InlineData("<fmxmlsnippet>")]
    [InlineData("<a /><b />")]
    [InlineData("<!DOCTYPE a [<!ENTITY secret SYSTEM 'file:///private/credential'>]><a>&secret;</a>")]
    public void MalformedXmlAndDtds_AreRejectedWithoutSensitiveErrors(string input)
    {
        var result = ClipboardPayloadInterpreter.Interpret(Encoding.UTF8.GetBytes(input));
        var probe = result.XmlHypotheses[0];
        Assert.Equal("Valid", probe.Utf8Status);
        Assert.Equal("Invalid", probe.XmlStatus);
        Assert.DoesNotContain("credential", probe.Detail!);
        Assert.DoesNotContain("file:", probe.Detail!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Utf8Bom_IsObservedAndAcceptedAtBothOffsets(int offset)
    {
        var bytes = new byte[offset].Concat(Encoding.UTF8.GetPreamble()).Concat(Encoding.UTF8.GetBytes(Xml)).ToArray();
        var result = ClipboardPayloadInterpreter.Interpret(bytes);
        var probe = Assert.Single(result.XmlHypotheses.Where(p => p.Offset == offset));
        Assert.Equal("UTF-8", probe.Bom);
        Assert.Equal("Valid", probe.XmlStatus);
    }

    [Fact]
    public void LargePayloads_SkipExpensiveProbes_ButKeepHeaderObservations()
    {
        var bytes = new byte[ClipboardPayloadInterpreter.MaxProbeBytes + 5];
        var result = ClipboardPayloadInterpreter.Interpret(bytes);
        Assert.Equal(64, result.LeadingHex.Length);
        Assert.All(result.XmlHypotheses, p =>
        {
            Assert.Equal("Skipped", p.Utf8Status);
            Assert.Equal("NotAttempted", p.XmlStatus);
            Assert.Contains("full payload retained", p.Detail!);
        });
    }

    [Fact]
    public void LongSnippetType_IsAbbreviatedOnlyInSummary()
    {
        var input = Encoding.UTF8.GetBytes($"<fmxmlsnippet type=\"{new string('a', 1000)}\" />");
        var result = ClipboardPayloadInterpreter.Interpret(input);
        Assert.Contains("summary truncated", result.XmlHypotheses[0].SnippetType!);
        Assert.Equal(1000, Encoding.UTF8.GetString(input).Count(c => c == 'a'));
    }
}
