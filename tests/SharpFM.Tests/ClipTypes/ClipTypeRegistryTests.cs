using SharpFM.Model.ClipTypes;
using SharpFM.Model.Parsing;

namespace SharpFM.Tests.ClipTypes;

public class ClipTypeRegistryTests : IDisposable
{
    public ClipTypeRegistryTests()
    {
        ClipTypeRegistry.Reset();
    }

    public void Dispose()
    {
        ClipTypeRegistry.Reset();
    }

    [Fact]
    public void For_UnknownFormat_FallsBackToOpaque()
    {
        var strategy = ClipTypeRegistry.For("Mac-XMNOPE");
        Assert.Same(OpaqueClipStrategy.Instance, strategy);
    }

    [Fact]
    public void Register_ThenLookup_ReturnsRegistered()
    {
        var fake = new FakeStrategy("Mac-XMFAKE");
        ClipTypeRegistry.Register(fake);

        Assert.Same(fake, ClipTypeRegistry.For("Mac-XMFAKE"));
    }

    [Fact]
    public void Register_DuplicateId_OverwritesPrior()
    {
        var first = new FakeStrategy("Mac-XMFAKE");
        var second = new FakeStrategy("Mac-XMFAKE");
        ClipTypeRegistry.Register(first);
        ClipTypeRegistry.Register(second);

        Assert.Same(second, ClipTypeRegistry.For("Mac-XMFAKE"));
    }

    [Fact]
    public void IsRegistered_ReflectsExplicitRegistration()
    {
        Assert.False(ClipTypeRegistry.IsRegistered("Mac-XMFAKE"));
        ClipTypeRegistry.Register(new FakeStrategy("Mac-XMFAKE"));
        Assert.True(ClipTypeRegistry.IsRegistered("Mac-XMFAKE"));
    }

    [Fact]
    public void All_DoesNotIncludeOpaqueFallback()
    {
        Assert.Empty(ClipTypeRegistry.All);
        ClipTypeRegistry.Register(new FakeStrategy("Mac-XMFAKE"));
        Assert.Single(ClipTypeRegistry.All);
    }

    [Fact]
    public void Reset_ClearsAllRegistrations()
    {
        ClipTypeRegistry.Register(new FakeStrategy("Mac-XMFAKE"));
        ClipTypeRegistry.Reset();
        Assert.Empty(ClipTypeRegistry.All);
        Assert.Same(OpaqueClipStrategy.Instance, ClipTypeRegistry.For("Mac-XMFAKE"));
    }

    private sealed class FakeStrategy(string formatId) : IClipTypeStrategy
    {
        public string FormatId { get; } = formatId;
        public string DisplayName => "Fake";
        public ClipParseResult Parse(string xml) =>
            new ParseSuccess(new OpaqueClipModel(xml), ClipParseReport.Empty);
        public string DefaultXml(string clipName) => "<x/>";
    }
}
