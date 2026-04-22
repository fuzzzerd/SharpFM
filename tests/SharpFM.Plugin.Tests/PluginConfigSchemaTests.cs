using SharpFM.Plugin;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class PluginConfigSchemaTests
{
    [Fact]
    public void Empty_HasZeroFields()
    {
        Assert.Empty(PluginConfigSchema.Empty.Fields);
    }

    [Fact]
    public void Empty_IsStableSingleton()
    {
        // Same reference on every access — callers can compare without allocation concerns.
        Assert.Same(PluginConfigSchema.Empty, PluginConfigSchema.Empty);
    }

    [Theory]
    [InlineData(PluginConfigFieldType.String)]
    [InlineData(PluginConfigFieldType.MultilineString)]
    [InlineData(PluginConfigFieldType.Bool)]
    [InlineData(PluginConfigFieldType.Int)]
    [InlineData(PluginConfigFieldType.Double)]
    [InlineData(PluginConfigFieldType.Enum)]
    public void Field_RoundTripsAllProperties(PluginConfigFieldType type)
    {
        var enumValues = type == PluginConfigFieldType.Enum ? new[] { "a", "b" } : null;
        var field = new PluginConfigField(
            Key: "k",
            Label: "My Label",
            Type: type,
            DefaultValue: "def",
            Description: "desc",
            EnumValues: enumValues);

        Assert.Equal("k", field.Key);
        Assert.Equal("My Label", field.Label);
        Assert.Equal(type, field.Type);
        Assert.Equal("def", field.DefaultValue);
        Assert.Equal("desc", field.Description);
        Assert.Equal(enumValues, field.EnumValues);
    }
}
