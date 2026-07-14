using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

public class DisplayQuotingTests
{
    [Theory]
    [InlineData("Refresh", "\"Refresh\"")]
    [InlineData("", "\"\"")]
    [InlineData("O\"Brien", "\"O\"\"Brien\"")]
    [InlineData(" Padded ", "\" Padded \"")]
    [InlineData("looks\" (#9) done", "\"looks\"\" (#9) done\"")]
    public void Quote_DoublesEmbeddedQuotes(string name, string expected) =>
        Assert.Equal(expected, DisplayQuoting.Quote(name));

    [Theory]
    [InlineData("Refresh", 4, "\"Refresh\" (#4)")]
    [InlineData("Refresh", 0, "\"Refresh\"")]
    [InlineData("O\"Brien", 5, "\"O\"\"Brien\" (#5)")]
    public void QuoteWithId_SuppressesZeroSentinel(string name, int id, string expected) =>
        Assert.Equal(expected, DisplayQuoting.QuoteWithId(name, id));

    [Theory]
    [InlineData("\"Refresh\"", "Refresh")]
    [InlineData("\"\"", "")]
    [InlineData("\"O\"\"Brien\"", "O\"Brien")]
    [InlineData("\" Padded \"", " Padded ")]
    public void TryParseQuoted_UndoublesEmbeddedQuotes(string token, string expectedName)
    {
        Assert.True(DisplayQuoting.TryParseQuoted(token, out var name));
        Assert.Equal(expectedName, name);
    }

    [Theory]
    [InlineData("Refresh")]
    [InlineData("\"Refresh")]
    [InlineData("Refresh\"")]
    [InlineData("")]
    public void TryParseQuoted_RejectsUnquotedTokens(string token) =>
        Assert.False(DisplayQuoting.TryParseQuoted(token, out _));

    [Theory]
    [InlineData("\"Refresh\" (#4)", "Refresh", 4)]
    [InlineData("\"O\"\"Brien\" (#5)", "O\"Brien", 5)]
    [InlineData("\"looks\"\" (#9) done\" (#7)", "looks\" (#9) done", 7)]
    public void TryParseQuotedWithId_UndoublesEmbeddedQuotesAndParsesId(string token, string expectedName, int expectedId)
    {
        Assert.True(DisplayQuoting.TryParseQuotedWithId(token, out var name, out var id));
        Assert.Equal(expectedName, name);
        Assert.Equal(expectedId, id);
    }

    [Theory]
    [InlineData("\"Refresh\"")]
    [InlineData("Refresh (#4)")]
    public void TryParseQuotedWithId_RejectsTokensMissingEitherPart(string token) =>
        Assert.False(DisplayQuoting.TryParseQuotedWithId(token, out _, out _));

    [Theory]
    [InlineData("Refresh")]
    [InlineData("O\"Brien")]
    [InlineData("")]
    [InlineData(" Padded ")]
    [InlineData("looks\" (#9) done")]
    public void QuoteThenParse_RoundTrips(string name)
    {
        Assert.True(DisplayQuoting.TryParseQuoted(DisplayQuoting.Quote(name), out var parsed));
        Assert.Equal(name, parsed);
    }

    [Theory]
    [InlineData("Refresh", 4)]
    [InlineData("O\"Brien", 5)]
    [InlineData("looks\" (#9) done", 7)]
    public void QuoteWithIdThenParse_RoundTrips(string name, int id)
    {
        Assert.True(DisplayQuoting.TryParseQuotedWithId(DisplayQuoting.QuoteWithId(name, id), out var parsedName, out var parsedId));
        Assert.Equal(name, parsedName);
        Assert.Equal(id, parsedId);
    }

    [Theory]
    [InlineData("\"Refresh\" (#4)", "Refresh", 4)]
    [InlineData("\"O\"\"Brien\"", "O\"Brien", 0)]
    public void TryParseNamedRef_PrefersIdSuffixOverBareForm(string token, string expectedName, int expectedId)
    {
        Assert.True(DisplayQuoting.TryParseNamedRef(token, out var namedRef));
        Assert.Equal(new NamedRef(expectedId, expectedName), namedRef);
    }

    [Fact]
    public void TryParseNamedRef_RejectsUnquotedToken()
    {
        Assert.False(DisplayQuoting.TryParseNamedRef("Refresh", out var namedRef));
        Assert.Equal(new NamedRef(0, ""), namedRef);
    }
}
