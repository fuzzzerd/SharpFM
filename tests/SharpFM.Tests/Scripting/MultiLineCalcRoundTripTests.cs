using System.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Steps;
using SharpFM.Model.Scripting.Values;
using SharpFM.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting;

/// <summary>
/// Round-trip coverage for steps whose calculation contains literal
/// newlines. The render contract: every continuation line of a multi-line
/// step is indented to the column just after the step's opening '['.
/// The parse contract: that exact prefix is stripped before re-joining,
/// so any user-authored extra indent inside the calc survives byte-for-byte.
/// </summary>
public class MultiLineCalcRoundTripTests
{
    [Fact]
    public void Renders_else_if_case_calc_with_aligned_continuation()
    {
        // Capture mirrors the FM Pro fixture in docs/step-definitions.md.
        var calc = new Calculation("Case ( $a > 1; 1;\n$b > 3; 4 )");
        var step = new ElseIfStep(enabled: true, condition: calc);
        var script = new FmScript([step]);

        var lines = script.ToDisplayLines();

        // Two visual lines for one logical step.
        Assert.Equal(2, lines.Length);
        Assert.Equal("Else If [ Case ( $a > 1; 1;", lines[0]);

        // Continuation aligns to the column just after '[ ' on line 0.
        var bracketCol = lines[0].IndexOf('[') + 2;
        Assert.Equal(new string(' ', bracketCol) + "$b > 3; 4 ) ]", lines[1]);
    }

    [Fact]
    public void Renders_nested_block_preserves_per_step_bracket_column()
    {
        // Set Variable nested inside two If blocks. Continuation alignment
        // is measured per-step, not from the outermost block — so the
        // inner Set Variable's continuation column reflects ITS bracket,
        // not the outer If's.
        var outerIf = new IfStep(true, new Calculation("$outer > 0"));
        var innerIf = new IfStep(true, new Calculation("$inner > 0"));
        var setVar = new SetVariableStep(
            enabled: true,
            name: "$x",
            value: new Calculation("Let ( a = 1 ;\na + 1 )"),
            repetition: new Calculation("1"));
        var endInner = new EndIfStep(true);
        var endOuter = new EndIfStep(true);

        var script = new FmScript([outerIf, innerIf, setVar, endInner, endOuter]);
        var lines = script.ToDisplayLines();

        // Find the Set Variable's first line (8-space block indent: 2 levels deep).
        var setVarLine = lines.First(l => l.TrimStart().StartsWith("Set Variable"));
        var setVarLineIndex = System.Array.IndexOf(lines, setVarLine);
        var continuationLine = lines[setVarLineIndex + 1];

        var bracketCol = setVarLine.IndexOf('[') + 2;
        Assert.StartsWith("        Set Variable [", setVarLine); // 8-space block indent
        Assert.Equal(new string(' ', bracketCol) + "a + 1 ) ]", continuationLine);
    }

    [Fact]
    public void Parses_round_trip_preserves_bracket_aligned_continuation()
    {
        // Render → parse should recover the exact Calculation.Text, with
        // no leftover render-side indent injected into the calc.
        var original = new Calculation("Case ( $a > 1; 1;\n$b > 3; 4 )");
        var step = new ElseIfStep(true, original);
        var script1 = new FmScript([step]);

        var displayText = script1.ToDisplayText();
        var script2 = ScriptTextParser.FromDisplayText(displayText);

        Assert.Single(script2.Steps);
        var roundTripped = Assert.IsType<ElseIfStep>(script2.Steps[0]);
        Assert.Equal(original.Text, roundTripped.Condition.Text);
    }

    [Fact]
    public void Parses_preserves_user_authored_extra_indent_inside_calc()
    {
        // Calc contains user-authored deeper indent inside Let bindings.
        // The render-side prefix on continuation lines is the bracket
        // column; anything *beyond* that is the user's calc formatting
        // and must round-trip verbatim.
        var calc = new Calculation("Let ( [\n        a = 1 ;\n        b = 2\n      ] ; a + b )");
        var step = new SetVariableStep(true, "$x", calc, new Calculation("1"));
        var script1 = new FmScript([step]);

        var displayText = script1.ToDisplayText();
        var script2 = ScriptTextParser.FromDisplayText(displayText);

        var roundTripped = Assert.IsType<SetVariableStep>(script2.Steps[0]);
        Assert.Equal(calc.Text, roundTripped.Value.Text);
    }

    [Fact]
    public void Parses_tolerates_underindented_continuation()
    {
        // User manually deleted leading whitespace from a continuation.
        // The parser should strip what's there (not crash, not over-strip).
        var displayText = "If [ Case ( $a > 1;\n1; 0 ) ]"; // continuation has no leading spaces
        var script = ScriptTextParser.FromDisplayText(displayText);

        Assert.Single(script.Steps);
        var ifStep = Assert.IsType<IfStep>(script.Steps[0]);
        // Expected merge keeps the newline; calc text starts at "Case (".
        Assert.Equal("Case ( $a > 1;\n1; 0 )", ifStep.Condition.Text);
    }

    [Fact]
    public void Full_round_trip_realistic_multi_line_script()
    {
        // Combined fixture: top-level multi-line, then a nested multi-line.
        var script1 = new FmScript([
            new IfStep(true, new Calculation("$x > 0")),
            new ElseIfStep(true, new Calculation("Case ( $a > 1; 1;\n$b > 3; 4 )")),
            new SetVariableStep(true, "$y", new Calculation("Let ( a = 1 ;\na + 1 )"), new Calculation("1")),
            new EndIfStep(true),
        ]);

        var displayText1 = script1.ToDisplayText();
        var script2 = ScriptTextParser.FromDisplayText(displayText1);
        var displayText2 = script2.ToDisplayText();

        Assert.Equal(displayText1, displayText2);
        Assert.Equal(script1.Steps.Count, script2.Steps.Count);
    }
}
