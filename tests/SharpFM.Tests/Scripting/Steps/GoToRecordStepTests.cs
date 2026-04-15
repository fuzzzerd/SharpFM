using System.Xml.Linq;
using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GoToRecordStepTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    private const string FirstXml =
        "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
        + "<NoInteract state=\"False\"></NoInteract>"
        + "<RowPageLocation value=\"First\"></RowPageLocation>"
        + "</Step>";

    private const string NextExitOnXml =
        "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
        + "<NoInteract state=\"False\"></NoInteract>"
        + "<Exit state=\"True\"></Exit>"
        + "<RowPageLocation value=\"Next\"></RowPageLocation>"
        + "</Step>";

    private const string NextExitOffXml =
        "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
        + "<NoInteract state=\"False\"></NoInteract>"
        + "<Exit state=\"False\"></Exit>"
        + "<RowPageLocation value=\"Next\"></RowPageLocation>"
        + "</Step>";

    private const string PreviousExitOnXml =
        "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
        + "<NoInteract state=\"False\"></NoInteract>"
        + "<Exit state=\"True\"></Exit>"
        + "<RowPageLocation value=\"Previous\"></RowPageLocation>"
        + "</Step>";

    private const string LastXml =
        "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
        + "<NoInteract state=\"False\"></NoInteract>"
        + "<RowPageLocation value=\"Last\"></RowPageLocation>"
        + "</Step>";

    private const string ByCalcDialogOnXml =
        "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
        + "<NoInteract state=\"False\"></NoInteract>"
        + "<RowPageLocation value=\"ByCalculation\"></RowPageLocation>"
        + "<Calculation><![CDATA[$someVar + 3]]></Calculation>"
        + "</Step>";

    private const string ByCalcDialogOffXml =
        "<Step enable=\"True\" id=\"16\" name=\"Go to Record/Request/Page\">"
        + "<NoInteract state=\"True\"></NoInteract>"
        + "<RowPageLocation value=\"ByCalculation\"></RowPageLocation>"
        + "<Calculation><![CDATA[$someVar + 3]]></Calculation>"
        + "</Step>";

    [Fact]
    public void First_Display_Bare()
    {
        var step = ScriptStep.FromXml(MakeStep(FirstXml));
        Assert.Equal("Go to Record/Request/Page [ First ]", step.ToDisplayLine());
    }

    [Fact]
    public void Last_Display_Bare()
    {
        var step = ScriptStep.FromXml(MakeStep(LastXml));
        Assert.Equal("Go to Record/Request/Page [ Last ]", step.ToDisplayLine());
    }

    [Fact]
    public void NextExitOn_Display_ShowsExitOn()
    {
        var step = ScriptStep.FromXml(MakeStep(NextExitOnXml));
        Assert.Equal(
            "Go to Record/Request/Page [ Next ; Exit after last: On ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void NextExitOff_Display_ShowsExitOff()
    {
        var step = ScriptStep.FromXml(MakeStep(NextExitOffXml));
        Assert.Equal(
            "Go to Record/Request/Page [ Next ; Exit after last: Off ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void PreviousExitOn_Display_ShowsExitOn()
    {
        var step = ScriptStep.FromXml(MakeStep(PreviousExitOnXml));
        Assert.Equal(
            "Go to Record/Request/Page [ Previous ; Exit after last: On ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void ByCalc_DialogOn_RendersInvertedNoInteract()
    {
        var step = ScriptStep.FromXml(MakeStep(ByCalcDialogOnXml));
        Assert.Equal(
            "Go to Record/Request/Page [ By calculation: $someVar + 3 ; With dialog: On ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void ByCalc_DialogOff_RendersInvertedNoInteract()
    {
        var step = ScriptStep.FromXml(MakeStep(ByCalcDialogOffXml));
        Assert.Equal(
            "Go to Record/Request/Page [ By calculation: $someVar + 3 ; With dialog: Off ]",
            step.ToDisplayLine());
    }

    [Fact]
    public void NextExitOn_RoundTrip_ExitElementEmitted()
    {
        var step = ScriptStep.FromXml(MakeStep(NextExitOnXml));
        var xml = step.ToXml();
        Assert.NotNull(xml.Element("Exit"));
        Assert.Equal("True", xml.Element("Exit")!.Attribute("state")!.Value);
    }

    [Fact]
    public void First_RoundTrip_NoExitElement()
    {
        // Exit element is emitted only for Next/Previous.
        var step = ScriptStep.FromXml(MakeStep(FirstXml));
        var xml = step.ToXml();
        Assert.Null(xml.Element("Exit"));
    }

    [Fact]
    public void ByCalc_RoundTrip_PreservesCalculationAndNoInteractState()
    {
        var step = ScriptStep.FromXml(MakeStep(ByCalcDialogOffXml));
        var xml = step.ToXml();

        Assert.Equal("ByCalculation",
            xml.Element("RowPageLocation")!.Attribute("value")!.Value);
        Assert.Equal("$someVar + 3", xml.Element("Calculation")!.Value);
        Assert.Equal("True", xml.Element("NoInteract")!.Attribute("state")!.Value);
    }

    [Fact]
    public void NoInteract_AlwaysEmitted_RegardlessOfLocation()
    {
        var firstStep = ScriptStep.FromXml(MakeStep(FirstXml));
        Assert.NotNull(firstStep.ToXml().Element("NoInteract"));

        var nextStep = ScriptStep.FromXml(MakeStep(NextExitOnXml));
        Assert.NotNull(nextStep.ToXml().Element("NoInteract"));

        var byCalcStep = ScriptStep.FromXml(MakeStep(ByCalcDialogOnXml));
        Assert.NotNull(byCalcStep.ToXml().Element("NoInteract"));
    }

    [Fact]
    public void FullRoundTrip_ByCalcDialogOff_PreservesAll()
    {
        var step1 = ScriptStep.FromXml(MakeStep(ByCalcDialogOffXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();

        Assert.Equal("ByCalculation",
            xml.Element("RowPageLocation")!.Attribute("value")!.Value);
        Assert.Equal("$someVar + 3", xml.Element("Calculation")!.Value);
        Assert.Equal("True", xml.Element("NoInteract")!.Attribute("state")!.Value);
    }
}
