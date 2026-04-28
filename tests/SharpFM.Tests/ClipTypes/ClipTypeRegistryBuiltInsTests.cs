using SharpFM.Model.ClipTypes;

namespace SharpFM.Tests.ClipTypes;

[Collection(RegistryMutatingCollection.Name)]
public class ClipTypeRegistryBuiltInsTests : IDisposable
{
    public ClipTypeRegistryBuiltInsTests()
    {
        ClipTypeRegistry.Reset();
        ClipTypeRegistry.RegisterBuiltIns();
    }

    public void Dispose()
    {
        ClipTypeRegistry.Reset();
        ClipTypeRegistry.RegisterBuiltIns();
    }

    [Theory]
    [InlineData("Mac-XMSS", "Script Steps")]
    [InlineData("Mac-XMSC", "Script")]
    [InlineData("Mac-XMTB", "Table")]
    [InlineData("Mac-XMFD", "Field")]
    [InlineData("Mac-XML2", "Layout")]
    public void RegisterBuiltIns_RegistersExpectedFormat(string formatId, string displayName)
    {
        Assert.True(ClipTypeRegistry.IsRegistered(formatId));
        Assert.Equal(displayName, ClipTypeRegistry.For(formatId).DisplayName);
    }

    [Fact]
    public void RegisterBuiltIns_DoesNotRegisterOpaque()
    {
        Assert.False(ClipTypeRegistry.IsRegistered(OpaqueClipStrategy.Instance.FormatId));
    }

    [Fact]
    public void RegisterBuiltIns_IsIdempotent()
    {
        ClipTypeRegistry.RegisterBuiltIns();
        Assert.Equal(5, ClipTypeRegistry.All.Count);
    }
}
