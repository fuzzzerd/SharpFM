using SharpFM.Model.ClipTypes;

namespace SharpFM.Tests.ClipTypes;

public class ClipTypeRegistryTests
{
    [Fact]
    public void For_UnknownFormat_FallsBackToOpaque()
    {
        Assert.Same(OpaqueClipStrategy.Instance, ClipTypeRegistry.For("Mac-XMNOPE"));
    }

    [Fact]
    public void For_KnownFormat_ReturnsBuiltInStrategy()
    {
        Assert.Same(ScriptClipStrategy.Steps, ClipTypeRegistry.For("Mac-XMSS"));
    }
}
