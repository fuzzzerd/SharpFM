using System.Linq;
using System.Text;
using SharpFM.Model;
using Xunit;

namespace SharpFM.Tests.Core;

public class FileMakerClipTests
{
    private const string SimpleXml = "<fmxmlsnippet type=\"FMObjectList\"><Step id=\"1\" name=\"Test\"/></fmxmlsnippet>";
    private const string TableXml =
        "<fmxmlsnippet type=\"FMObjectList\">"
        + "<BaseTable name=\"People\">"
        + "<Field id=\"1\" name=\"FirstName\" dataType=\"Text\" fieldType=\"Normal\">"
        + "<Validation><NotEmpty value=\"true\"/><Unique value=\"false\"/></Validation>"
        + "<Comment>first name field</Comment>"
        + "</Field>"
        + "<Field id=\"2\" name=\"Age\" dataType=\"Number\" fieldType=\"Normal\">"
        + "<Validation><NotEmpty value=\"false\"/><Unique value=\"false\"/></Validation>"
        + "</Field>"
        + "</BaseTable></fmxmlsnippet>";

    private const string LayoutXml =
        "<fmxmlsnippet type=\"FMObjectList\">"
        + "<Layout name=\"Detail\">"
        + "<Object type=\"Field\"><FieldObj><Name>People::FirstName</Name></FieldObj></Object>"
        + "<Object type=\"Field\"><FieldObj><Name>People::LastName</Name></FieldObj></Object>"
        + "<Object type=\"Button\"><ButtonObj/></Object>"
        + "</Layout></fmxmlsnippet>";

    // --- String constructor ---

    [Fact]
    public void Constructor_String_SetsNameAndFormat()
    {
        var clip = new FileMakerClip("MyClip", "Mac-XMSS", SimpleXml);
        Assert.Equal("MyClip", clip.Name);
        Assert.Equal("Mac-XMSS", clip.ClipboardFormat);
    }

    [Fact]
    public void Constructor_String_PrettifiesXml()
    {
        var clip = new FileMakerClip("test", "Mac-XMSS", SimpleXml);
        Assert.Contains("\n", clip.XmlData);
    }

    [Fact]
    public void Constructor_String_InvalidXml_StoresRawString()
    {
        var clip = new FileMakerClip("test", "Mac-XMSS", "not xml at all");
        Assert.Equal("not xml at all", clip.XmlData);
    }

    // --- Byte array constructor ---

    [Fact]
    public void Constructor_ByteArray_ExtractsNameFromXml()
    {
        var xmlBytes = Encoding.UTF8.GetBytes(SimpleXml);
        var lengthPrefix = System.BitConverter.GetBytes(xmlBytes.Length);
        var data = lengthPrefix.Concat(xmlBytes).ToArray();

        var clip = new FileMakerClip("fallback", "Mac-XMSS", data);
        Assert.Contains("fmxmlsnippet", clip.XmlData);
    }

    [Fact]
    public void Constructor_ByteArray_EmptyPayload_SetsEmptyXml()
    {
        // 4 bytes length prefix + no actual data
        var data = System.BitConverter.GetBytes(0).Concat(Encoding.UTF8.GetBytes("")).ToArray();
        var clip = new FileMakerClip("fallback", "Mac-XMSS", data);
        Assert.Equal("fallback", clip.Name);
    }

    // --- RawData round-trip ---

    [Fact]
    public void RawData_ContainsLengthPrefixAndXml()
    {
        var clip = new FileMakerClip("test", "Mac-XMSS", SimpleXml);
        var rawData = clip.RawData;

        var length = System.BitConverter.ToInt32(rawData, 0);
        var xmlPart = Encoding.UTF8.GetString(rawData, 4, length);

        Assert.Equal(clip.XmlData, xmlPart);
    }

    [Fact]
    public void RawData_InvalidatedWhenXmlChanges()
    {
        var clip = new FileMakerClip("test", "Mac-XMSS", SimpleXml);
        var first = clip.RawData;

        clip.XmlData = "<new/>";
        var second = clip.RawData;

        Assert.NotEqual(first, second);
    }

    // --- ClipTypes ---

    [Fact]
    public void ClipTypes_ContainsExpectedFormats()
    {
        Assert.Contains(FileMakerClip.ClipTypes, ct => ct.KeyId == "Mac-XMSS" && ct.DisplayName == "ScriptSteps");
        Assert.Contains(FileMakerClip.ClipTypes, ct => ct.KeyId == "Mac-XMTB" && ct.DisplayName == "Table");
        Assert.Contains(FileMakerClip.ClipTypes, ct => ct.KeyId == "Mac-XML2" && ct.DisplayName == "Layout");
    }

    // --- Fields property ---

    [Fact]
    public void Fields_TableClip_ReturnsFieldsWithMetadata()
    {
        var clip = new FileMakerClip("People", "Mac-XMTB", TableXml);
        var fields = clip.Fields.ToList();

        Assert.Equal(2, fields.Count);
        Assert.Equal("FirstName", fields[0].Name);
        Assert.Equal("Text", fields[0].DataType);
        Assert.True(fields[0].NotEmpty);
        Assert.Equal("first name field", fields[0].Comment);
        Assert.Equal("Age", fields[1].Name);
        Assert.Equal("Number", fields[1].DataType);
    }

    [Fact]
    public void Fields_LayoutClip_ReturnsFieldNames()
    {
        var clip = new FileMakerClip("Detail", "Mac-XML2", LayoutXml);
        var fields = clip.Fields.ToList();

        Assert.Equal(2, fields.Count);
        Assert.Equal("FirstName", fields[0].Name);
        Assert.Equal("LastName", fields[1].Name);
    }

    [Fact]
    public void Fields_ScriptStepsClip_ReturnsEmpty()
    {
        var clip = new FileMakerClip("test", "Mac-XMSS", SimpleXml);
        Assert.Empty(clip.Fields);
    }

    [Fact]
    public void Fields_UnknownFormat_ReturnsEmpty()
    {
        var clip = new FileMakerClip("test", "Unknown-Format", SimpleXml);
        Assert.Empty(clip.Fields);
    }

    // --- ClipBytesToPrettyXml ---

    [Fact]
    public void ClipBytesToPrettyXml_ValidXml_ReturnsPrettyVersion()
    {
        var bytes = Encoding.UTF8.GetBytes("<root><child/></root>");
        var result = FileMakerClip.ClipBytesToPrettyXml(bytes);
        Assert.Contains("\n", result);
        Assert.Contains("child", result);
    }

    [Fact]
    public void ClipBytesToPrettyXml_EmptyInput_ReturnsEmpty()
    {
        var result = FileMakerClip.ClipBytesToPrettyXml(System.Array.Empty<byte>());
        Assert.Equal(string.Empty, result);
    }
}
