using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Parsing;

namespace SharpFM.Tests.Parsing;

public class ClipParseLocationResolverTests
{
    [Fact]
    public void NestedElement_Resolves()
    {
        const string xml = "<fmxmlsnippet><Step name=\"A\"/><Step name=\"B\"><Inner/></Step></fmxmlsnippet>";

        var result = ClipParseLocationResolver.Resolve(xml, "/fmxmlsnippet/Step[2]/Inner[1]");

        Assert.Contains("Inner", result);
    }

    [Fact]
    public void Attribute_Resolves()
    {
        const string xml = "<root attr=\"value\"/>";

        var result = ClipParseLocationResolver.Resolve(xml, "/root/@attr");

        Assert.Equal("attr=\"value\"", result);
    }

    [Fact]
    public void BareSlash_ResolvesToWholeDocument()
    {
        const string xml = "<root><child/></root>";

        var result = ClipParseLocationResolver.Resolve(xml, "/");

        Assert.Contains("root", result);
    }

    [Fact]
    public void RootOnlyPath_ResolvesToRootElement()
    {
        const string xml = "<root attr=\"1\"><child/></root>";

        var result = ClipParseLocationResolver.Resolve(xml, "/root");

        Assert.Contains("root", result);
        Assert.Contains("attr", result);
    }

    [Fact]
    public void MissingChild_FallsBackGracefully()
    {
        const string xml = "<root/>";

        var result = ClipParseLocationResolver.Resolve(xml, "/root/Missing[1]");

        Assert.Equal("(not found in current XML)", result);
    }

    [Fact]
    public void MismatchedRootName_FallsBackGracefully()
    {
        const string xml = "<root/>";

        var result = ClipParseLocationResolver.Resolve(xml, "/somethingElse");

        Assert.Equal("(not found in current XML)", result);
    }

    [Fact]
    public void OrphanOutputElementLocation_FallsBackGracefully()
    {
        // XmlRoundTripDiff emits a bare, index-less segment for elements that
        // exist only in the round-tripped output, never in the source XML
        // this resolver walks — so it can never be found here.
        const string xml = "<root/>";

        var result = ClipParseLocationResolver.Resolve(xml, "/root/DefaultedChild");

        Assert.Equal("(not found in current XML)", result);
    }

    [Fact]
    public void MissingAttribute_FallsBackGracefully()
    {
        const string xml = "<root/>";

        var result = ClipParseLocationResolver.Resolve(xml, "/root/@missing");

        Assert.Equal("(not found in current XML)", result);
    }

    [Fact]
    public void MalformedXml_FallsBackGracefully()
    {
        const string xml = "<root><unterminated>";

        var result = ClipParseLocationResolver.Resolve(xml, "/root");

        Assert.Equal("(not found in current XML)", result);
    }

    [Fact]
    public void LongResult_IsTruncated()
    {
        var longValue = new string('x', 500);
        var xml = $"<root attr=\"{longValue}\"/>";

        var result = ClipParseLocationResolver.Resolve(xml, "/root/@attr");

        Assert.True(result.Length < 500);
        Assert.EndsWith("…", result);
    }

    // The tests above assert the resolver's own path grammar in isolation.
    // These instead feed it real XmlRoundTripDiff.Compute output, so a future
    // change to that path-building format (XmlRoundTripDiff.cs) that the
    // resolver doesn't also track would show up as a failure here.

    [Fact]
    public void RealAttributeMismatchDiagnostic_Resolves()
    {
        var input = XElement.Parse("<root attr=\"a\"/>");
        var output = XElement.Parse("<root attr=\"b\"/>");
        var diag = XmlRoundTripDiff.Compute(input, output).Single();

        var result = ClipParseLocationResolver.Resolve(input.ToString(), diag.Location);

        Assert.Equal("attr=\"a\"", result);
    }

    [Fact]
    public void RealUnmodeledNestedElementDiagnostic_Resolves()
    {
        var input = XElement.Parse("<fmxmlsnippet><Step><Mystery/></Step></fmxmlsnippet>");
        var output = XElement.Parse("<fmxmlsnippet><Step/></fmxmlsnippet>");
        var diag = XmlRoundTripDiff.Compute(input, output).Single();

        var result = ClipParseLocationResolver.Resolve(input.ToString(), diag.Location);

        Assert.Contains("Mystery", result);
    }

    [Fact]
    public void RealDroppedNamespaceDiagnostic_Resolves()
    {
        var input = XElement.Parse("<root xmlns:x=\"urn:x\"><x:thing/></root>");
        var output = XElement.Parse("<root><thing/></root>");
        var diag = XmlRoundTripDiff.Compute(input, output)
            .Single(d => d.Kind == ParseDiagnosticKind.DroppedNamespace);

        var result = ClipParseLocationResolver.Resolve(input.ToString(), diag.Location);

        Assert.Contains("root", result);
    }

    [Fact]
    public void RealOrphanOutputElementDiagnostic_FallsBackGracefully()
    {
        // Only exists in the round-tripped output, never in the source XML
        // this resolver walks — confirms the fallback for the real shape
        // XmlRoundTripDiff emits, not just a hand-typed approximation of it.
        var input = XElement.Parse("<root/>");
        var output = XElement.Parse("<root><DefaultedChild/></root>");
        var diag = XmlRoundTripDiff.Compute(input, output).Single();

        var result = ClipParseLocationResolver.Resolve(input.ToString(), diag.Location);

        Assert.Equal("(not found in current XML)", result);
    }
}
