using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class ShowCustomDialogStepTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    private const string MinimalXml =
        "<Step enable=\"True\" id=\"87\" name=\"Show Custom Dialog\">"
        + "<Title><Calculation><![CDATA[\"Title Calculation or Text\"]]></Calculation></Title>"
        + "<Message><Calculation><![CDATA[\"Body calculation or text\"]]></Calculation></Message>"
        + "<Buttons>"
        + "<Button CommitState=\"True\"><Calculation><![CDATA[\"OK\"]]></Calculation></Button>"
        + "<Button CommitState=\"False\"></Button>"
        + "<Button CommitState=\"False\"></Button>"
        + "</Buttons>"
        + "</Step>";

    private const string CustomButtonsXml =
        "<Step enable=\"True\" id=\"87\" name=\"Show Custom Dialog\">"
        + "<Title><Calculation><![CDATA[\"Title Calculation or Text\"]]></Calculation></Title>"
        + "<Message><Calculation><![CDATA[\"Body calculation or text\"]]></Calculation></Message>"
        + "<Buttons>"
        + "<Button CommitState=\"True\"><Calculation><![CDATA[\"OK\"]]></Calculation></Button>"
        + "<Button CommitState=\"False\"><Calculation><![CDATA[\"Cancel\"]]></Calculation></Button>"
        + "<Button CommitState=\"False\"><Calculation><![CDATA[\"Abort\"]]></Calculation></Button>"
        + "</Buttons>"
        + "</Step>";

    private const string InputsWithRepetitionXml =
        "<Step enable=\"True\" id=\"87\" name=\"Show Custom Dialog\">"
        + "<Title><Calculation><![CDATA[\"Title Calculation or Text\"]]></Calculation></Title>"
        + "<Message><Calculation><![CDATA[\"Body calculation or text\"]]></Calculation></Message>"
        + "<Buttons>"
        + "<Button CommitState=\"True\"><Calculation><![CDATA[\"OK\"]]></Calculation></Button>"
        + "<Button CommitState=\"False\"></Button>"
        + "<Button CommitState=\"False\"></Button>"
        + "</Buttons>"
        + "<InputFields>"
        + "<InputField UsePasswordCharacter=\"False\">"
        + "<Field table=\"ScriptDefinitionHelper\" id=\"3\" repetition=\"44\" name=\"CreatedBy\"></Field>"
        + "<Label><Calculation><![CDATA[\"Input Box Label Calculation\"]]></Calculation></Label>"
        + "</InputField>"
        + "<InputField UsePasswordCharacter=\"False\">"
        + "<Field repetition=\"2\">$$secondInputVariable</Field>"
        + "<Label><Calculation><![CDATA[\"Second Label Calculation\"]]></Calculation></Label>"
        + "</InputField>"
        + "<InputField UsePasswordCharacter=\"False\">"
        + "<Field table=\"ScriptDefinitionHelper\" id=\"3\" name=\"CreatedBy\"></Field>"
        + "<Label><Calculation><![CDATA[\"Third input label calculation\"]]></Calculation></Label>"
        + "</InputField>"
        + "</InputFields>"
        + "</Step>";

    [Fact]
    public void Minimal_Display_TitleMessageButDefaultButtonsSuppressed()
    {
        var step = ScriptStep.FromXml(MakeStep(MinimalXml));
        var display = step.ToDisplayLine();

        Assert.StartsWith("Show Custom Dialog [ Title: \"Title Calculation or Text\"", display);
        Assert.Contains("Message: \"Body calculation or text\"", display);
        // The default 3-slot button shape is suppressed from display because
        // it round-trips identically without being emitted. See
        // IsDefaultButtonShape in ShowCustomDialogStep.
        Assert.DoesNotContain("Buttons:", display);
        Assert.DoesNotContain("Inputs:", display);
    }

    [Fact]
    public void DefaultButtons_RoundTrip_PreservesThreeSlotsInXml()
    {
        // Display suppresses the Buttons block for default shapes, but the
        // XML writer must still emit all three slots. Verifies the round-
        // trip pair between IsDefaultButtonShape (suppress) and
        // DefaultButtons (rehydrate).
        var step1 = ScriptStep.FromXml(MakeStep(MinimalXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();

        var buttons = xml.Element("Buttons")!.Elements("Button").ToArray();
        Assert.Equal(3, buttons.Length);
        Assert.Equal("True", buttons[0].Attribute("CommitState")!.Value);
        Assert.Equal("\"OK\"", buttons[0].Element("Calculation")!.Value);
        Assert.Equal("False", buttons[1].Attribute("CommitState")!.Value);
        Assert.Null(buttons[1].Element("Calculation"));
    }

    [Fact]
    public void CustomButtons_Display_AllThreeSlotsWithCommitKeywords()
    {
        var step = ScriptStep.FromXml(MakeStep(CustomButtonsXml));
        var display = step.ToDisplayLine();

        Assert.Contains(
            "Buttons: [ \"OK\" commit ; \"Cancel\" nocommit ; \"Abort\" nocommit ]",
            display);
    }

    [Fact]
    public void InputsWithRepetition_Display_IncludesRepAndTargets()
    {
        var step = ScriptStep.FromXml(MakeStep(InputsWithRepetitionXml));
        var display = step.ToDisplayLine();

        Assert.Contains("Inputs: [", display);
        Assert.Contains("ScriptDefinitionHelper::CreatedBy[44]", display);
        Assert.Contains("$$secondInputVariable[2]", display);
        Assert.Contains("\"Input Box Label Calculation\" plain", display);
    }

    [Fact]
    public void Minimal_RoundTrip_PreservesThreeButtonSlots()
    {
        var step = ScriptStep.FromXml(MakeStep(MinimalXml));
        var xml = step.ToXml();

        var buttons = xml.Element("Buttons")!.Elements("Button").ToArray();
        Assert.Equal(3, buttons.Length);

        Assert.Equal("True", buttons[0].Attribute("CommitState")!.Value);
        Assert.Equal("\"OK\"", buttons[0].Element("Calculation")!.Value);
        Assert.Equal("False", buttons[1].Attribute("CommitState")!.Value);
        Assert.Null(buttons[1].Element("Calculation"));
    }

    [Fact]
    public void CustomButtons_RoundTrip_AllLabelsAndCommitStatesPreserved()
    {
        var step = ScriptStep.FromXml(MakeStep(CustomButtonsXml));
        var xml = step.ToXml();

        var buttons = xml.Element("Buttons")!.Elements("Button").ToArray();
        Assert.Equal("\"OK\"", buttons[0].Element("Calculation")!.Value);
        Assert.Equal("\"Cancel\"", buttons[1].Element("Calculation")!.Value);
        Assert.Equal("\"Abort\"", buttons[2].Element("Calculation")!.Value);

        Assert.Equal("True", buttons[0].Attribute("CommitState")!.Value);
        Assert.Equal("False", buttons[1].Attribute("CommitState")!.Value);
        Assert.Equal("False", buttons[2].Attribute("CommitState")!.Value);
    }

    [Fact]
    public void InputsWithRepetition_RoundTrip_PreservesRepetitionAttribute()
    {
        var step = ScriptStep.FromXml(MakeStep(InputsWithRepetitionXml));
        var xml = step.ToXml();

        var inputs = xml.Element("InputFields")!.Elements("InputField").ToArray();
        Assert.Equal(3, inputs.Length);

        var firstField = inputs[0].Element("Field")!;
        Assert.Equal("44", firstField.Attribute("repetition")!.Value);
        Assert.Equal("CreatedBy", firstField.Attribute("name")!.Value);

        var secondField = inputs[1].Element("Field")!;
        Assert.Equal("2", secondField.Attribute("repetition")!.Value);
        Assert.Equal("$$secondInputVariable", secondField.Value);

        var thirdField = inputs[2].Element("Field")!;
        Assert.Null(thirdField.Attribute("repetition"));
    }

    [Fact]
    public void Minimal_RoundTrip_OmitsInputFieldsContainer()
    {
        var step = ScriptStep.FromXml(MakeStep(MinimalXml));
        var xml = step.ToXml();
        Assert.Null(xml.Element("InputFields"));
    }

    [Fact]
    public void FullRoundTrip_CustomButtons_PreservesAll()
    {
        // Round-trip through the display extension should survive button
        // labels and CommitState — the whole point of the form-3 extension.
        var step1 = ScriptStep.FromXml(MakeStep(CustomButtonsXml));
        var display = step1.ToDisplayLine();
        var step2 = SharpFM.Scripting.ScriptTextParser.FromDisplayLine(display);
        var xml = step2.ToXml();

        var buttons = xml.Element("Buttons")!.Elements("Button").ToArray();
        Assert.Equal(3, buttons.Length);
        Assert.Equal("\"OK\"", buttons[0].Element("Calculation")!.Value);
        Assert.Equal("\"Cancel\"", buttons[1].Element("Calculation")!.Value);
        Assert.Equal("\"Abort\"", buttons[2].Element("Calculation")!.Value);
        Assert.Equal("True", buttons[0].Attribute("CommitState")!.Value);
        Assert.Equal("False", buttons[1].Attribute("CommitState")!.Value);
        Assert.Equal("False", buttons[2].Attribute("CommitState")!.Value);
    }
}
