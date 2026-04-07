using SharpFM.Model;
using Xunit;

namespace SharpFM.Tests.Core;

public class FileMakerClipExtensionsTests
{
    private static FileMakerClip MakeTableClip(string tableName, params (string name, string dataType)[] fields)
    {
        var fieldElements = string.Join("", fields.Select(f =>
            $"<Field id=\"1\" name=\"{f.name}\" dataType=\"{f.dataType}\" fieldType=\"Normal\">"
            + "<Validation><NotEmpty value=\"true\"/><Unique value=\"false\"/></Validation>"
            + "</Field>"));

        var xml = $"<fmxmlsnippet type=\"FMObjectList\"><BaseTable name=\"{tableName}\">{fieldElements}</BaseTable></fmxmlsnippet>";
        return new FileMakerClip(tableName, "Mac-XMTB", xml);
    }

    [Fact]
    public void CreateClass_TextFields_GeneratesStringProperties()
    {
        var clip = MakeTableClip("People", ("FirstName", "Text"), ("LastName", "Text"));
        var code = clip.CreateClass();

        Assert.Contains("class People", code);
        Assert.Contains("string FirstName { get; set; }", code);
        Assert.Contains("string LastName { get; set; }", code);
    }

    [Fact]
    public void CreateClass_NumberField_GeneratesIntProperty()
    {
        var clip = MakeTableClip("Items", ("Quantity", "Number"));
        var code = clip.CreateClass();

        Assert.Contains("int Quantity { get; set; }", code);
    }

    [Fact]
    public void CreateClass_DateField_GeneratesDateTimeProperty()
    {
        var clip = MakeTableClip("Events", ("EventDate", "Date"));
        var code = clip.CreateClass();

        Assert.Contains("DateTime EventDate { get; set; }", code);
    }

    [Fact]
    public void CreateClass_TimeField_GeneratesTimeSpanProperty()
    {
        var clip = MakeTableClip("Events", ("StartTime", "Time"));
        var code = clip.CreateClass();

        Assert.Contains("TimeSpan StartTime { get; set; }", code);
    }

    [Fact]
    public void CreateClass_TimestampField_GeneratesDateTimeProperty()
    {
        var clip = MakeTableClip("Log", ("CreatedAt", "TimeStamp"));
        var code = clip.CreateClass();

        Assert.Contains("DateTime CreatedAt { get; set; }", code);
    }

    [Fact]
    public void CreateClass_BinaryField_GeneratesByteArrayProperty()
    {
        var clip = MakeTableClip("Docs", ("Photo", "Binary"));
        var code = clip.CreateClass();

        Assert.Contains("byte[] Photo { get; set; }", code);
    }

    [Fact]
    public void CreateClass_UnknownDataType_DefaultsToString()
    {
        var clip = MakeTableClip("Misc", ("Custom", "SomeOtherType"));
        var code = clip.CreateClass();

        Assert.Contains("string Custom { get; set; }", code);
    }

    [Fact]
    public void CreateClass_IncludesDataContractAttribute()
    {
        var clip = MakeTableClip("People", ("Name", "Text"));
        var code = clip.CreateClass();

        Assert.Contains("[DataContract]", code);
        Assert.Contains("[DataMember]", code);
    }

    [Fact]
    public void CreateClass_IncludesNamespaceAndUsings()
    {
        var clip = MakeTableClip("People", ("Name", "Text"));
        var code = clip.CreateClass();

        Assert.Contains("namespace SharpFM.CodeGen", code);
        Assert.Contains("using System;", code);
        Assert.Contains("using System.Runtime.Serialization;", code);
    }

    [Fact]
    public void CreateClass_WithFieldProjectionList_FiltersFields()
    {
        var clip = MakeTableClip("People", ("FirstName", "Text"), ("LastName", "Text"), ("Age", "Number"));
        var code = clip.CreateClass(new[] { "FirstName", "Age" });

        Assert.Contains("FirstName", code);
        Assert.Contains("Age", code);
        Assert.DoesNotContain("LastName", code);
    }

    [Fact]
    public void CreateClass_NullClip_ReturnsEmpty()
    {
        FileMakerClip? clip = null;
        var code = FileMakerClipExtensions.CreateClass(clip!, (FileMakerClip?)null);
        Assert.Equal(string.Empty, code);
    }

    [Fact]
    public void CreateClass_NullableNumberField_GetsQuestionMark()
    {
        // NotEmpty=false on a non-string type should produce a nullable
        var xml = "<fmxmlsnippet type=\"FMObjectList\"><BaseTable name=\"T\">"
            + "<Field id=\"1\" name=\"Score\" dataType=\"Number\" fieldType=\"Normal\">"
            + "<Validation><NotEmpty value=\"false\"/><Unique value=\"false\"/></Validation>"
            + "</Field></BaseTable></fmxmlsnippet>";
        var clip = new FileMakerClip("T", "Mac-XMTB", xml);
        var code = clip.CreateClass();

        Assert.Contains("int?", code);
    }
}
