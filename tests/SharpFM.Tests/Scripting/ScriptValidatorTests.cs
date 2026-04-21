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
    public void UnknownStep_ProducesWarning()
    {
        // Unknown steps are preserved verbatim via RawStep; the warning
        // alerts the user that display-text edits won't round-trip and
        // they should go through the XML editor instead.
        var diagnostics = ScriptValidator.Validate("FakeStep [ param ]");
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostics[0].Severity);
        Assert.Contains("Unknown script step", diagnostics[0].Message);
        Assert.Contains("XML editor", diagnostics[0].Message);
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
        var param = new SharpFM.Model.Scripting.Registry.ParamMetadata
        {
            Name = "x", XmlElement = "X", Type = "boolean",
        };
        var valid = SharpFM.Model.Scripting.Registry.StepRegistry.GetValidValues(param);
        Assert.Contains("On", valid);
        Assert.Contains("Off", valid);
    }

    [Fact]
    public void GetValidValues_ExplicitValidValues_ReturnsThose()
    {
        var param = new SharpFM.Model.Scripting.Registry.ParamMetadata
        {
            Name = "x", XmlElement = "X", Type = "enum",
            ValidValues = ["Alpha", "Beta"],
        };
        var valid = SharpFM.Model.Scripting.Registry.StepRegistry.GetValidValues(param);
        Assert.Equal(new[] { "Alpha", "Beta" }, valid);
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

    [Fact]
    public void MultiLineCalc_ContinuationLines_NotFlaggedAsUnknownSteps()
    {
        // Regression: continuation lines of a multi-line calc were being
        // validated as separate steps and failing the catalog lookup,
        // producing spurious "Unknown script step" red squiggles on every
        // continuation line of a multi-line If/Else If/Set Variable etc.
        var script =
            "Else If [ Case ( $a > 1; 1;\n" +
            "                 $b > 3; 4 ) ]\n" +
            "End If";
        // The Else If is unmatched (no opening If); that's the only legit
        // diagnostic. Continuation line 2 must NOT produce an "Unknown
        // script step" error for "$b > 3; 4 ) ]".
        var diagnostics = ScriptValidator.Validate(script);

        Assert.DoesNotContain(diagnostics, d => d.Message.Contains("Unknown script step"));
    }

    [Fact]
    public void MultiLineSetVariable_ValidatesCleanly()
    {
        // Realistic multi-line Set Variable inside an If block — should
        // produce zero diagnostics.
        var script =
            "If [ $x > 0 ]\n" +
            "    Set Variable [ $y ; Value: Let ( a = 1 ;\n" +
            "                                     a + 1 ) ]\n" +
            "End If";
        var diagnostics = ScriptValidator.Validate(script);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ExportFieldContents_FieldReference_NotFlagged()
    {
        // Regression: positional-match validator used to skip non-enum
        // params and check the field reference against a later enum's
        // valid values, producing a false-positive warning on
        // "Assets::Selected File Container".
        var script = "Export Field Contents [ Assets::Selected File Container ; $PATH ; Create folders: Off ]";
        var diagnostics = ScriptValidator.Validate(script);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void PositionalEnum_StillFlaggedWhenInvalid()
    {
        // Counter-check: a truly invalid enum value still flags. Find
        // Matching Records's first positional is Replace/Constrain/Extend.
        var script = "Find Matching Records [ Bogus ; Customer::name ]";
        var diagnostics = ScriptValidator.Validate(script);
        Assert.NotEmpty(diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostics[0].Severity);
    }

    [Fact]
    public void InsertFromUrl_SelectFlagToken_NotFlagged()
    {
        // Regression: "Select" is a flag-style presence marker for the
        // SelectAll boolean param, not a value. The validator used to
        // check "Select" against ["On", "Off"] and fail.
        var script = "Insert from URL [ Select ; With dialog: Off ; $url ]";
        var diagnostics = ScriptValidator.Validate(script);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void InsertFromUrl_AllFlagTokens_NotFlagged()
    {
        // Every flag token on Insert from URL is a bare HrLabel: "Select",
        // "Verify SSL Certificates". None should warn.
        var script = "Insert from URL [ Select ; With dialog: Off ; $url ; Verify SSL Certificates ]";
        var diagnostics = ScriptValidator.Validate(script);
        Assert.Empty(diagnostics);
    }
}
