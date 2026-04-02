using System.Linq;
using SharpFM.Scripting;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class ScriptValidatorTests
{
    [Fact]
    public void ValidScript_NoDiagnostics()
    {
        var script = "# Comment\nSet Variable [ $x ; Value: 1 ]\nIf [ $x > 0 ]\n    Beep\nEnd If";
        var diagnostics = ScriptValidator.Validate(script);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void UnknownStep_ProducesError()
    {
        var diagnostics = ScriptValidator.Validate("FakeStep [ param ]");
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.Contains("Unknown script step", diagnostics[0].Message);
    }

    [Fact]
    public void UnmatchedIf_ProducesError()
    {
        var diagnostics = ScriptValidator.Validate("If [ $x > 0 ]\n    Beep");
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.Contains("no matching closing step", diagnostics[0].Message);
    }

    [Fact]
    public void UnmatchedEndIf_ProducesError()
    {
        var diagnostics = ScriptValidator.Validate("End If");
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostics[0].Severity);
        Assert.Contains("without matching opening step", diagnostics[0].Message);
    }

    [Fact]
    public void MatchedIfEndIf_NoDiagnostics()
    {
        var diagnostics = ScriptValidator.Validate("If [ $x > 0 ]\nEnd If");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NestedBlocks_NoDiagnostics()
    {
        var script = "If [ $x > 0 ]\n    Loop\n        Beep\n    End Loop\nEnd If";
        var diagnostics = ScriptValidator.Validate(script);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EmptyInput_NoDiagnostics()
    {
        var diagnostics = ScriptValidator.Validate("");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void CommentsOnly_NoDiagnostics()
    {
        var diagnostics = ScriptValidator.Validate("# just a comment\n# another comment");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void GetValidValues_BooleanParam_ReturnsOnOff()
    {
        var param = new StepParam { Type = "boolean" };
        var valid = ScriptValidator.GetValidValues(param);
        Assert.Contains("On", valid);
        Assert.Contains("Off", valid);
    }

    [Fact]
    public void GetValidValues_BooleanWithHrEnumValues_ReturnsThose()
    {
        var param = new StepParam
        {
            Type = "boolean",
            HrEnumValues = new System.Collections.Generic.Dictionary<string, string?>
            {
                { "True", "On" },
                { "False", "Off" }
            }
        };
        var valid = ScriptValidator.GetValidValues(param);
        Assert.Contains("On", valid);
        Assert.Contains("Off", valid);
        Assert.DoesNotContain("True", valid);
    }

    [Fact]
    public void ElseWithoutIf_ProducesError()
    {
        var diagnostics = ScriptValidator.Validate("Else");
        Assert.Single(diagnostics);
        Assert.Contains("without matching opening step", diagnostics[0].Message);
    }

    [Fact]
    public void MultipleErrors_AllReported()
    {
        var script = "FakeStep1\nFakeStep2\nIf [ 1 ]";
        var diagnostics = ScriptValidator.Validate(script);
        // 2 unknown steps + 1 unclosed If
        Assert.Equal(3, diagnostics.Count);
    }

    [Fact]
    public void DisabledUnknownStep_StillFlagged()
    {
        var diagnostics = ScriptValidator.Validate("// FakeStep [ param ]");
        Assert.Single(diagnostics);
        Assert.Contains("Unknown script step", diagnostics[0].Message);
    }
}
