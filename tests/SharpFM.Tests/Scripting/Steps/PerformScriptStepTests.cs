using System.Xml.Linq;
using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class PerformScriptStepTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    private const string ByRefWithParamXml =
        "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
        + "<Calculation><![CDATA[$$SomeGlobalVariable]]></Calculation>"
        + "<Script id=\"4\" name=\"Dummy-Script-For-Reference\"></Script>"
        + "</Step>";

    private const string ByCalcWithParamXml =
        "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
        + "<Calculated><Calculation><![CDATA[$$globalVar & \" literal-string\"]]></Calculation></Calculated>"
        + "<Calculation><![CDATA[$$SomeGlobalVariable]]></Calculation>"
        + "</Step>";

    private const string ByRefNoParamXml =
        "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
        + "<Script id=\"4\" name=\"Dummy-Script-For-Reference\"></Script>"
        + "</Step>";

    private const string ByCalcNoParamXml =
        "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
        + "<Calculated><Calculation><![CDATA[$$globalVar & \" literal-string\"]]></Calculation></Calculated>"
        + "</Step>";

    [Fact]
    public void ByRefWithParam_Display_IncludesIdSuffixAndParameterLabel()
    {
        var step = ScriptStep.FromXml(MakeStep(ByRefWithParamXml));
        Assert.Equal(
            "Perform Script [ \"Dummy-Script-For-Reference\" (#4) ; Parameter: $$SomeGlobalVariable ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void ByCalcWithParam_Display_UsesByNameLabel()
    {
        var step = ScriptStep.FromXml(MakeStep(ByCalcWithParamXml));
        Assert.Equal(
            "Perform Script [ By name: $$globalVar & \" literal-string\" ; Parameter: $$SomeGlobalVariable ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void ByRefNoParam_Display_OmitsParameterLabel()
    {
        var step = ScriptStep.FromXml(MakeStep(ByRefNoParamXml));
        Assert.Equal(
            "Perform Script [ \"Dummy-Script-For-Reference\" (#4) ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void ByCalcNoParam_Display_OmitsParameterLabel()
    {
        var step = ScriptStep.FromXml(MakeStep(ByCalcNoParamXml));
        Assert.Equal(
            "Perform Script [ By name: $$globalVar & \" literal-string\" ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void ByRefWithParam_RoundTrip_PreservesScriptIdAndParam()
    {
        var step = ScriptStep.FromXml(MakeStep(ByRefWithParamXml));
        var xml = step.ToXml();

        var script = xml.Element("Script");
        Assert.NotNull(script);
        Assert.Equal("4", script!.Attribute("id")!.Value);
        Assert.Equal("Dummy-Script-For-Reference", script.Attribute("name")!.Value);

        Assert.Equal("$$SomeGlobalVariable", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void ByCalcWithParam_RoundTrip_PreservesCalculatedAndParam()
    {
        var step = ScriptStep.FromXml(MakeStep(ByCalcWithParamXml));
        var xml = step.ToXml();

        Assert.NotNull(xml.Element("Calculated"));
        Assert.Equal("$$globalVar & \" literal-string\"",
            xml.Element("Calculated")!.Element("Calculation")!.Value);
        Assert.Null(xml.Element("Script"));

        Assert.Equal("$$SomeGlobalVariable", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void ByRefNoParam_RoundTrip_OmitsParameterElement()
    {
        var step = ScriptStep.FromXml(MakeStep(ByRefNoParamXml));
        var xml = step.ToXml();

        // Parameter absence is by element-missing, not empty CDATA.
        Assert.Null(xml.Element("Calculation"));
        Assert.NotNull(xml.Element("Script"));
    }

    [Fact]
    public void ByCalcNoParam_RoundTrip_OmitsParameterElementAndScript()
    {
        var step = ScriptStep.FromXml(MakeStep(ByCalcNoParamXml));
        var xml = step.ToXml();

        Assert.NotNull(xml.Element("Calculated"));
        Assert.Null(xml.Element("Calculation"));
        Assert.Null(xml.Element("Script"));
    }

    [Fact]
    public void FullRoundTrip_ByRef_PreservesScriptId()
    {
        var step1 = ScriptStep.FromXml(MakeStep(ByRefWithParamXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();

        var script = xml.Element("Script");
        Assert.NotNull(script);
        Assert.Equal("4", script!.Attribute("id")!.Value);
        Assert.Equal("Dummy-Script-For-Reference", script.Attribute("name")!.Value);
    }
}
