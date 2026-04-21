using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// IfStep pilot: calc + block-pair POCO with an intentional
/// drop of the <c>Restore</c> param per the zero-loss audit
/// in docs/advanced-filemaker-scripting-syntax.md.
/// </summary>
public class IfStepTests
{
    // Canonical shape from agentic-fm's snippet — note the
    // <Restore state="False"/> element that FM Pro itself never emits.
    private const string AgenticFmSnippet = """
        <?xml version="1.0"?>
        <fmxmlsnippet type="FMObjectList">
          <Step enable="True" id="68" name="If">
            <Restore state="False"/>
            <Calculation><![CDATA[$error <> 0]]></Calculation>
          </Step>
        </fmxmlsnippet>
        """;

    // FM Pro clipboard form — no <Restore> element at all.
    private const string FmProStyleStep = """
        <Step enable="True" id="68" name="If"><Calculation><![CDATA[$error <> 0]]></Calculation></Step>
        """;

    private static XElement StepFromSnippet(string snippet) =>
        XDocument.Parse(snippet).Root!.Element("Step")!;

    [Fact]
    public void RoundTrip_WithRestoreInSource_DropsRestore()
    {
        // Input has <Restore/>; output must not. This codifies the
        // intentional drop described in the zero-loss audit.
        var source = StepFromSnippet(AgenticFmSnippet);
        var step = IfStep.Metadata.FromXml!(source);
        var output = step.ToXml();

        Assert.Null(output.Element("Restore"));
        Assert.Equal("$error <> 0", output.Element("Calculation")!.Value);
    }

    [Fact]
    public void RoundTrip_FmProStyle_IsByteIdentical()
    {
        var source = XElement.Parse(FmProStyleStep);
        var step = IfStep.Metadata.FromXml!(source);

        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Display_EmitsConditionInBrackets()
    {
        var step = new IfStep(true, new Calculation("$error <> 0"));
        Assert.Equal("If [ $error <> 0 ]", step.ToDisplayLine());
    }

    [Fact]
    public void Registry_HasIfWithBlockPair()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("If", out var metadata));
        Assert.Equal(68, metadata!.Id);
        Assert.Equal("control", metadata.Category);
        Assert.NotNull(metadata.BlockPair);
        Assert.Equal(BlockPairRole.Open, metadata.BlockPair!.Role);
        Assert.Contains("End If", metadata.BlockPair.Partners);
        Assert.Contains("Else", metadata.BlockPair.Partners);
        Assert.Contains("Else If", metadata.BlockPair.Partners);
    }

    [Fact]
    public void Metadata_Params_DescribeCalculation()
    {
        var metadata = StepRegistry.ByName["If"];
        var param = Assert.Single(metadata.Params);
        Assert.Equal("Calculation", param.XmlElement);
        Assert.Equal("calculation", param.Type);
    }
}
