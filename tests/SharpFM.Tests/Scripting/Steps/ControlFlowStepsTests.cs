using System.Xml.Linq;
using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ControlFlowStepsTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    private const string IfXml =
        "<Step enable=\"True\" id=\"68\" name=\"If\">"
        + "<Calculation><![CDATA[$variable > $anotherVariable]]></Calculation>"
        + "</Step>";

    private const string ElseIfMultiLineXml =
        "<Step enable=\"True\" id=\"125\" name=\"Else If\">"
        + "<Calculation><![CDATA[Case ( $a > 1; 1;\n$b > 3; 4 )]]></Calculation>"
        + "</Step>";

    private const string ElseXml =
        "<Step enable=\"True\" id=\"69\" name=\"Else\"></Step>";

    private const string EndIfXml =
        "<Step enable=\"True\" id=\"70\" name=\"End If\"></Step>";

    private const string LoopXml =
        "<Step enable=\"True\" id=\"71\" name=\"Loop\"></Step>";

    private const string EndLoopXml =
        "<Step enable=\"True\" id=\"73\" name=\"End Loop\"></Step>";

    private const string ExitLoopIfXml =
        "<Step enable=\"True\" id=\"72\" name=\"Exit Loop If\">"
        + "<Calculation><![CDATA[$variable = $condition]]></Calculation>"
        + "</Step>";

    // The failing-regression case: complex calc with parens and angles
    // used to be rendered as `If [ Off ]` by the generic catalog path.
    private const string IfComplexCalcXml =
        "<Step enable=\"True\" id=\"68\" name=\"If\">"
        + "<Calculation><![CDATA[Get ( FoundCount ) > 0]]></Calculation>"
        + "</Step>";

    [Fact]
    public void If_Display_UsesCalculationText()
    {
        var step = ScriptStep.FromXml(MakeStep(IfXml));
        Assert.Equal("If [ $variable > $anotherVariable ]", step.ToDisplayLine());
    }

    [Fact]
    public void If_ComplexCalc_Display_DoesNotCorruptToOff()
    {
        // Regression guard: the generic catalog path used to emit
        // `If [ Off ]` for calcs containing parens and comparison ops.
        // The typed POCO must render the raw calc text verbatim.
        var step = ScriptStep.FromXml(MakeStep(IfComplexCalcXml));
        Assert.Equal("If [ Get ( FoundCount ) > 0 ]", step.ToDisplayLine());
    }

    [Fact]
    public void ElseIf_MultiLineCalc_Display_CollapsesToSingleLine()
    {
        // Display line preserves literal newline chars as-is; consumers
        // render single-line but the XML round-trip keeps the newlines.
        var step = ScriptStep.FromXml(MakeStep(ElseIfMultiLineXml));
        var display = step.ToDisplayLine();
        Assert.StartsWith("Else If [ Case ( $a > 1; 1;", display);
        Assert.EndsWith("$b > 3; 4 ) ]", display);
    }

    [Fact]
    public void ElseIf_MultiLineCalc_RoundTrip_PreservesNewlines()
    {
        var step = ScriptStep.FromXml(MakeStep(ElseIfMultiLineXml));
        var xml = step.ToXml();
        Assert.Contains("\n", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void Else_Display_IsBareName()
    {
        var step = ScriptStep.FromXml(MakeStep(ElseXml));
        Assert.Equal("Else", step.ToDisplayLine());
    }

    [Fact]
    public void EndIf_Display_IsBareName()
    {
        var step = ScriptStep.FromXml(MakeStep(EndIfXml));
        Assert.Equal("End If", step.ToDisplayLine());
    }

    [Fact]
    public void If_RoundTrip_PreservesCalculationElement()
    {
        var step = ScriptStep.FromXml(MakeStep(IfXml));
        var xml = step.ToXml();
        Assert.Equal("If", xml.Attribute("name")!.Value);
        Assert.Equal("68", xml.Attribute("id")!.Value);
        Assert.Equal("$variable > $anotherVariable", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void ElseIf_RoundTrip_UsesCorrectIdAndName()
    {
        var step = ScriptStep.FromXml(MakeStep(ElseIfMultiLineXml));
        var xml = step.ToXml();
        Assert.Equal("Else If", xml.Attribute("name")!.Value);
        Assert.Equal("125", xml.Attribute("id")!.Value);
    }

    [Fact]
    public void Else_RoundTrip_NoChildElements()
    {
        var step = ScriptStep.FromXml(MakeStep(ElseXml));
        var xml = step.ToXml();
        Assert.Empty(xml.Elements());
        Assert.Equal("69", xml.Attribute("id")!.Value);
    }

    [Fact]
    public void EndIf_RoundTrip_NoChildElements()
    {
        var step = ScriptStep.FromXml(MakeStep(EndIfXml));
        var xml = step.ToXml();
        Assert.Empty(xml.Elements());
        Assert.Equal("70", xml.Attribute("id")!.Value);
    }

    [Fact]
    public void FullRoundTrip_IfComplexCalc_PreservesCalculation()
    {
        var step1 = ScriptStep.FromXml(MakeStep(IfComplexCalcXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();
        Assert.Equal("Get ( FoundCount ) > 0", xml.Element("Calculation")!.Value);
    }

    // --- Loop ---

    [Fact]
    public void Loop_Display_IsBareName()
    {
        var step = ScriptStep.FromXml(MakeStep(LoopXml));
        Assert.Equal("Loop", step.ToDisplayLine());
    }

    [Fact]
    public void Loop_Display_DoesNotShowCatalogDefaults()
    {
        // Regression guard: the generic catalog path rendered Loop as
        // `Loop [ Collapsed: Off ; Flush: Always ]` because it surfaced
        // catalog-default params FM Pro itself never emits. The typed
        // POCO bypasses the catalog renderer entirely.
        var step = ScriptStep.FromXml(MakeStep(LoopXml));
        var display = step.ToDisplayLine();
        Assert.DoesNotContain("Collapsed", display);
        Assert.DoesNotContain("Flush", display);
    }

    [Fact]
    public void Loop_RoundTrip_NoChildElements()
    {
        var step = ScriptStep.FromXml(MakeStep(LoopXml));
        var xml = step.ToXml();
        Assert.Empty(xml.Elements());
        Assert.Equal("71", xml.Attribute("id")!.Value);
    }

    // --- End Loop ---

    [Fact]
    public void EndLoop_Display_IsBareName()
    {
        var step = ScriptStep.FromXml(MakeStep(EndLoopXml));
        Assert.Equal("End Loop", step.ToDisplayLine());
    }

    [Fact]
    public void EndLoop_RoundTrip_NoChildElements()
    {
        var step = ScriptStep.FromXml(MakeStep(EndLoopXml));
        var xml = step.ToXml();
        Assert.Empty(xml.Elements());
        Assert.Equal("73", xml.Attribute("id")!.Value);
    }

    // --- Exit Loop If ---

    [Fact]
    public void ExitLoopIf_Display_UsesCalculationText()
    {
        var step = ScriptStep.FromXml(MakeStep(ExitLoopIfXml));
        Assert.Equal("Exit Loop If [ $variable = $condition ]", step.ToDisplayLine());
    }

    [Fact]
    public void ExitLoopIf_RoundTrip_PreservesCalculation()
    {
        var step = ScriptStep.FromXml(MakeStep(ExitLoopIfXml));
        var xml = step.ToXml();
        Assert.Equal("Exit Loop If", xml.Attribute("name")!.Value);
        Assert.Equal("72", xml.Attribute("id")!.Value);
        Assert.Equal("$variable = $condition", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void ExitLoopIf_FullRoundTrip_FromDisplay_PreservesCalc()
    {
        var step1 = ScriptStep.FromXml(MakeStep(ExitLoopIfXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();
        Assert.Equal("72", xml.Attribute("id")!.Value);
        Assert.Equal("$variable = $condition", xml.Element("Calculation")!.Value);
    }
}
