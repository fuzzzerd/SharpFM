using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Steps;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// IfStep pilot: calc + block-pair POCO. Per the vendored FileMaker XML skill
/// (§8.1), the canonical If emits <c>&lt;Restore state="False"/&gt;</c> before
/// the <c>&lt;Calculation&gt;</c>; SharpFM emits it and canonicalizes inputs
/// that omit it.
/// </summary>
public class IfStepTests
{
    // Canonical §8.1 shape — <Restore state="False"/> then <Calculation>.
    private const string CanonicalSnippet = """
        <?xml version="1.0"?>
        <fmxmlsnippet type="FMObjectList">
          <Step enable="True" id="68" name="If">
            <Restore state="False"/>
            <Calculation><![CDATA[$error <> 0]]></Calculation>
          </Step>
        </fmxmlsnippet>
        """;

    // Legacy input form that omitted <Restore>; emission canonicalizes it back in.
    private const string NoRestoreStep = """
        <Step enable="True" id="68" name="If"><Calculation><![CDATA[$error <> 0]]></Calculation></Step>
        """;

    private static XElement StepFromSnippet(string snippet) =>
        XDocument.Parse(snippet).Root!.Element("Step")!;

    [Fact]
    public void RoundTrip_WithRestoreInSource_PreservesRestore()
    {
        var source = StepFromSnippet(CanonicalSnippet);
        var step = IfStep.Parse(source);
        var output = step.ToXml();

        Assert.Equal("False", output.Element("Restore")!.Attribute("state")!.Value);
        Assert.Equal("$error <> 0", output.Element("Calculation")!.Value);
        // Restore precedes Calculation per canonical order.
        Assert.Equal(new[] { "Restore", "Calculation" },
            output.Elements().Select(e => e.Name.LocalName).ToArray());
    }

    [Fact]
    public void RoundTrip_NoRestoreInput_CanonicalizesByAddingRestore()
    {
        var source = XElement.Parse(NoRestoreStep);
        var step = IfStep.Parse(source);
        var output = step.ToXml();

        Assert.Equal("False", output.Element("Restore")!.Attribute("state")!.Value);
        Assert.Equal("$error <> 0", output.Element("Calculation")!.Value);
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
    public void Metadata_Shape_DescribesCalculation()
    {
        var metadata = StepRegistry.ByName["If"];
        var param = Assert.Single(ShapeHrView.HrNodes(metadata.Shape));
        Assert.IsType<BareCalcChild>(param);
        Assert.Equal("calculation", ShapeHrView.KindOf(param));
    }
}
