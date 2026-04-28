using SharpFM.Model.ClipTypes;
using SharpFM.Model.Parsing;

namespace SharpFM.Tests.ClipTypes;

public class ScriptClipStrategyTests
{
    [Fact]
    public void StepsAndScript_HaveDistinctIdentities()
    {
        Assert.Equal("Mac-XMSS", ScriptClipStrategy.Steps.FormatId);
        Assert.Equal("Mac-XMSC", ScriptClipStrategy.Script.FormatId);
        Assert.Equal("Script Steps", ScriptClipStrategy.Steps.DisplayName);
        Assert.Equal("Script", ScriptClipStrategy.Script.DisplayName);
    }

    [Fact]
    public void Parse_EmptyScriptSnippet_ReturnsLosslessSuccess()
    {
        var result = ScriptClipStrategy.Steps.Parse(
            "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>");

        var success = Assert.IsType<ParseSuccess>(result);
        Assert.IsType<ScriptClipModel>(success.Model);
        Assert.True(success.Report.IsLossless);
    }

    [Fact]
    public void Parse_KnownStep_RoundTripsLosslessly()
    {
        const string xml = """
            <fmxmlsnippet type="FMObjectList">
                <Step enable="True" id="141" name="Set Variable">
                    <Value><Calculation><![CDATA[1]]></Calculation></Value>
                    <Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>
                    <Name>$x</Name>
                </Step>
            </fmxmlsnippet>
            """;

        var result = ScriptClipStrategy.Steps.Parse(xml);

        var success = Assert.IsType<ParseSuccess>(result);
        Assert.True(success.Report.IsLossless,
            $"expected lossless parse, got: {string.Join("; ", success.Report.Diagnostics.Select(d => d.Message))}");
    }

    [Fact]
    public void Parse_UnknownStep_ReportsAsInfoButPreserves()
    {
        const string xml = """
            <fmxmlsnippet type="FMObjectList">
                <Step enable="True" id="99999" name="Future Step From FM 25">
                    <Mystery/>
                </Step>
            </fmxmlsnippet>
            """;

        var result = ScriptClipStrategy.Steps.Parse(xml);

        var success = Assert.IsType<ParseSuccess>(result);
        var unknownStep = success.Report.Diagnostics
            .Single(d => d.Kind == ParseDiagnosticKind.UnknownStep);
        Assert.Equal(ParseDiagnosticSeverity.Info, unknownStep.Severity);
        Assert.Contains("Future Step From FM 25", unknownStep.Message);
    }

    [Fact]
    public void Parse_MalformedXml_ReturnsFailure()
    {
        var result = ScriptClipStrategy.Steps.Parse("<fmxmlsnippet><not closed");

        var failure = Assert.IsType<ParseFailure>(result);
        Assert.Equal(ParseDiagnosticKind.XmlMalformed, failure.Report.Diagnostics[0].Kind);
    }

    [Fact]
    public void Parse_WrongRootElement_ReturnsFailureWithUnsupportedKind()
    {
        var result = ScriptClipStrategy.Steps.Parse("<wrongroot/>");

        var failure = Assert.IsType<ParseFailure>(result);
        Assert.Equal(ParseDiagnosticKind.UnsupportedClipType, failure.Report.Diagnostics[0].Kind);
    }

    [Fact]
    public void Parse_EmptyXml_ReturnsFailure()
    {
        var result = ScriptClipStrategy.Steps.Parse("");
        Assert.IsType<ParseFailure>(result);
    }

    [Fact]
    public void Parse_ScriptWrapperFormat_ReadsMetadata()
    {
        const string xml = """
            <fmxmlsnippet type="FMObjectList">
                <Script id="42" name="My Script" runFullAccess="False">
                    <Step enable="True" id="89" name="Halt Script"/>
                </Script>
            </fmxmlsnippet>
            """;

        var result = ScriptClipStrategy.Script.Parse(xml);

        var success = Assert.IsType<ParseSuccess>(result);
        var model = Assert.IsType<ScriptClipModel>(success.Model);
        Assert.NotNull(model.Script.Metadata);
        Assert.Equal("My Script", model.Script.Metadata!.Name);
    }

    [Fact]
    public void DefaultXml_ProducesParseableSnippet()
    {
        var seed = ScriptClipStrategy.Steps.DefaultXml("anything");
        var result = ScriptClipStrategy.Steps.Parse(seed);
        Assert.IsType<ParseSuccess>(result);
    }
}
