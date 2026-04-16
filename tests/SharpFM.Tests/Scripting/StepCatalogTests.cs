using System.Text.Json;
using Xunit;

namespace SharpFM.Tests.Scripting;

public class StepCatalogTests
{
    // --- BlockPairRole serialization (converter is on StepBlockPair.Role) ---

    [Theory]
    [InlineData("{\"role\":\"open\",\"partners\":[]}", BlockPairRole.Open)]
    [InlineData("{\"role\":\"middle\",\"partners\":[]}", BlockPairRole.Middle)]
    [InlineData("{\"role\":\"close\",\"partners\":[]}", BlockPairRole.Close)]
    public void BlockPairRole_Deserializes(string json, BlockPairRole expected)
    {
        var pair = JsonSerializer.Deserialize<StepBlockPair>(json);
        Assert.Equal(expected, pair!.Role);
    }

    [Theory]
    [InlineData(BlockPairRole.Open, "open")]
    [InlineData(BlockPairRole.Middle, "middle")]
    [InlineData(BlockPairRole.Close, "close")]
    public void BlockPairRole_Serializes(BlockPairRole value, string expected)
    {
        var pair = new StepBlockPair { Role = value, Partners = [] };
        var json = JsonSerializer.Serialize(pair);
        Assert.Contains($"\"role\":\"{expected}\"", json);
    }

    [Fact]
    public void BlockPairRole_Deserialize_Unknown_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<StepBlockPair>("{\"role\":\"unknown\",\"partners\":[]}"));
    }

    // --- StepDefinition record ---

    [Fact]
    public void StepDefinition_Deserializes_FromJson()
    {
        var json = """
        {
            "name": "Set Variable",
            "id": 141,
            "category": "Control",
            "selfClosing": true,
            "params": [
                { "xmlElement": "Name", "type": "text" }
            ]
        }
        """;

        var def = JsonSerializer.Deserialize<StepDefinition>(json);
        Assert.NotNull(def);
        Assert.Equal("Set Variable", def!.Name);
        Assert.Equal(141, def.Id);
        Assert.Equal("Control", def.Category);
        Assert.True(def.SelfClosing);
        Assert.Single(def.Params);
        Assert.Equal("Name", def.Params[0].XmlElement);
    }

    [Fact]
    public void StepDefinition_Defaults_AreCorrect()
    {
        var def = new StepDefinition();
        Assert.Equal("", def.Name);
        Assert.Null(def.Id);
        Assert.Equal("", def.Category);
        Assert.False(def.SelfClosing);
        Assert.Empty(def.Params);
        Assert.Null(def.BlockPair);
    }

    // --- StepParam record ---

    [Fact]
    public void StepParam_Deserializes_WithOptionalFields()
    {
        var json = """
        {
            "xmlElement": "Calculation",
            "type": "namedCalc",
            "hrLabel": "Value",
            "wrapperElement": "Value",
            "required": true,
            "invertedHr": true
        }
        """;

        var param = JsonSerializer.Deserialize<StepParam>(json);
        Assert.NotNull(param);
        Assert.Equal("Calculation", param!.XmlElement);
        Assert.Equal("namedCalc", param.Type);
        Assert.Equal("Value", param.HrLabel);
        Assert.Equal("Value", param.WrapperElement);
        Assert.True(param.Required);
        Assert.True(param.InvertedHr);
    }

    [Fact]
    public void StepParam_Defaults_AreCorrect()
    {
        var param = new StepParam();
        Assert.Equal("", param.XmlElement);
        Assert.Equal("", param.Type);
        Assert.Null(param.HrLabel);
        Assert.Null(param.XmlAttr);
        Assert.Null(param.WrapperElement);
        Assert.Null(param.ParentElement);
        Assert.False(param.Required);
        Assert.False(param.InvertedHr);
    }

    // --- StepBlockPair record ---

    [Fact]
    public void StepBlockPair_Deserializes()
    {
        var json = """{"role": "open", "partners": ["Else", "Else If", "End If"]}""";

        var pair = JsonSerializer.Deserialize<StepBlockPair>(json);
        Assert.NotNull(pair);
        Assert.Equal(BlockPairRole.Open, pair!.Role);
        Assert.Equal(3, pair.Partners.Length);
        Assert.Contains("Else", pair.Partners);
        Assert.Contains("End If", pair.Partners);
    }

    // --- StepParam with hrEnumValues ---

    [Fact]
    public void StepParam_HrEnumValues_Deserializes()
    {
        var json = """
        {
            "xmlElement": "RowPageLocation",
            "type": "enum",
            "hrEnumValues": { "1": "First", "2": "Last", "3": "Previous", "4": "Next" }
        }
        """;

        var param = JsonSerializer.Deserialize<StepParam>(json);
        Assert.NotNull(param!.HrEnumValues);
        Assert.Equal("First", param.HrEnumValues!["1"]);
        Assert.Equal(4, param.HrEnumValues.Count);
    }
}
