using System.Linq;
using System.Xml.Linq;
using SharpFM.Scripting;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class FmScriptModelTests
{
    private static string Wrap(string steps) =>
        $"<fmxmlsnippet type=\"FMObjectList\">{steps}</fmxmlsnippet>";

    [Fact]
    public void FromXml_ToDisplayText_Comment()
    {
        var xml = Wrap("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>hello</Text></Step>");
        var script = FmScript.FromXml(xml);
        Assert.Single(script.Steps);
        Assert.Equal("# hello", script.ToDisplayText());
    }

    [Fact]
    public void FromXml_ToDisplayText_IfEndIf_Indented()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"68\" name=\"If\"><Calculation><![CDATA[$x > 0]]></Calculation></Step>"
            + "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$y</Name></Step>"
            + "<Step enable=\"True\" id=\"70\" name=\"End If\"/>");
        var script = FmScript.FromXml(xml);
        var lines = script.ToDisplayLines();
        Assert.Equal(3, lines.Length);
        Assert.Equal("If [ $x > 0 ]", lines[0]);
        Assert.Equal("    Set Variable [ $y ; Value: 1 ]", lines[1]);
        Assert.Equal("End If", lines[2]);
    }

    [Fact]
    public void FromXml_ToDisplayText_DisabledStep()
    {
        var xml = Wrap("<Step enable=\"False\" id=\"93\" name=\"Beep\"/>");
        var script = FmScript.FromXml(xml);
        Assert.Equal("// Beep", script.ToDisplayText());
    }

    [Fact]
    public void FromXml_ScriptWrapper()
    {
        var xml = "<fmxmlsnippet type=\"FMObjectList\">"
            + "<Script id=\"1\" name=\"Test\">"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>inside</Text></Step>"
            + "</Script></fmxmlsnippet>";
        var script = FmScript.FromXml(xml);
        Assert.Single(script.Steps);
        Assert.Equal("# inside", script.ToDisplayText());
    }

    [Fact]
    public void FromDisplayText_ToXml_Comment()
    {
        var script = ScriptTextParser.FromDisplayText("# hello");
        var xml = script.ToXml();
        var doc = XDocument.Parse(xml);
        var step = doc.Root!.Element("Step")!;
        Assert.Equal("89", step.Attribute("id")?.Value);
        Assert.Equal("hello", step.Element("Text")?.Value);
    }

    [Fact]
    public void FromDisplayText_ToXml_SetVariable()
    {
        var script = ScriptTextParser.FromDisplayText("Set Variable [ $count ; Value: $count + 1 ]");
        var xml = script.ToXml();
        var doc = XDocument.Parse(xml);
        var step = doc.Root!.Element("Step")!;
        Assert.Equal("141", step.Attribute("id")?.Value);
        Assert.Equal("$count", step.Element("Name")?.Value);
    }

    [Fact]
    public void FromDisplayText_ToXml_OutputIsValid()
    {
        var scripts = new[]
        {
            "# comment",
            "Set Variable [ $x ; Value: 1 ]",
            "If [ $x > 0 ]\n    Beep\nEnd If",
            "Set Field [ T::F ; \"val\" ]",
            "// Beep",
        };

        foreach (var text in scripts)
        {
            var script = ScriptTextParser.FromDisplayText(text);
            var xml = script.ToXml();
            XDocument.Parse(xml); // should not throw
        }
    }

    [Fact]
    public void Validate_ValidScript_NoDiagnostics()
    {
        var script = ScriptTextParser.FromDisplayText(
            "# Comment\nSet Variable [ $x ; Value: 1 ]\nIf [ $x > 0 ]\n    Beep\nEnd If");
        var diagnostics = script.Validate();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validate_UnmatchedIf()
    {
        // Block pair validation is in ScriptValidator (needs text line positions)
        var diagnostics = ScriptValidator.Validate("If [ $x > 0 ]\n    Beep");
        Assert.Contains(diagnostics, d => d.Message.Contains("no matching closing step"));
    }

    [Fact]
    public void Validate_UnmatchedEndIf()
    {
        var diagnostics = ScriptValidator.Validate("End If");
        Assert.Contains(diagnostics, d => d.Message.Contains("without matching opening step"));
    }

    [Fact]
    public void RoundTrip_RealisticScript()
    {
        var original = "# Navigate and process records\n"
            + "Go to Layout [ \"Invoices\" ]\n"
            + "Perform Script [ \"Find Open Invoices\" ; Parameter: $status ]\n"
            + "If [ Get ( FoundCount ) > 0 ]\n"
            + "    Go to Record/Request/Page [ First ]\n"
            + "    Loop\n"
            + "        Set Field [ Invoices::Status ; \"Processed\" ]\n"
            + "        Set Variable [ $count ; Value: $count + 1 ]\n"
            + "        Go to Record/Request/Page [ Next ; Exit after last: On ]\n"
            + "    End Loop\n"
            + "    Show Custom Dialog [ Title: \"Done\" ; Message: $count & \" records processed\" ]\n"
            + "End If\n"
            + "Go to Layout [ original layout ]";

        // Display text → model → XML → model → display text
        var script1 = ScriptTextParser.FromDisplayText(original);
        var xml = script1.ToXml();
        XDocument.Parse(xml); // valid XML

        var script2 = FmScript.FromXml(xml);
        var roundTripped = script2.ToDisplayText();
        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void RoundTrip_XmlToDisplayToXml_PreservesStructure()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>test</Text></Step>"
            + "<Step enable=\"True\" id=\"68\" name=\"If\"><Calculation><![CDATA[$x > 0]]></Calculation></Step>"
            + "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Calculation><![CDATA[\"Done\"]]></Calculation>"
            + "<Field table=\"Invoices\" id=\"3\" name=\"Status\"/></Step>"
            + "<Step enable=\"True\" id=\"70\" name=\"End If\"/>");

        var script = FmScript.FromXml(xml);
        var display = script.ToDisplayText();
        var script2 = ScriptTextParser.FromDisplayText(display);

        Assert.Equal(script.Steps.Count, script2.Steps.Count);
        for (int i = 0; i < script.Steps.Count; i++)
        {
            Assert.Equal(
                script.Steps[i].Definition?.Name,
                script2.Steps[i].Definition?.Name);
            Assert.Equal(
                script.Steps[i].Enabled,
                script2.Steps[i].Enabled);
        }
    }

    [Fact]
    public void UpdateStep_ModifiesSingleStep()
    {
        var script = ScriptTextParser.FromDisplayText("# line one\nBeep\n# line three");
        Assert.Equal(3, script.Steps.Count);

        ScriptTextParser.UpdateStep(script, 1, "Set Variable [ $x ; Value: 1 ]");
        Assert.IsType<SharpFM.Model.Scripting.Steps.SetVariableStep>(script.Steps[1]);
        Assert.IsType<SharpFM.Model.Scripting.Steps.CommentStep>(script.Steps[0]);
        Assert.IsType<SharpFM.Model.Scripting.Steps.CommentStep>(script.Steps[2]);
    }

    [Fact]
    public void EmptyInput_NoDiagnostics()
    {
        var script = ScriptTextParser.FromDisplayText("");
        Assert.Empty(script.Steps);
        Assert.Empty(script.Validate());
    }

    [Fact]
    public void ToXml_EmptyScript()
    {
        var script = new FmScript(new System.Collections.Generic.List<ScriptStep>());
        var xml = script.ToXml();
        var doc = XDocument.Parse(xml);
        Assert.Equal("fmxmlsnippet", doc.Root!.Name.LocalName);
    }
}
