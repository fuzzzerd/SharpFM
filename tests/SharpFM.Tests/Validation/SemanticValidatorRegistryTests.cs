using System.Collections.Generic;
using SharpFM.Model.Parsing;
using SharpFM.Model.Scripting;
using SharpFM.Model.Validation;

namespace SharpFM.Tests.Validation;

public class SemanticValidatorRegistryTests
{
    [Fact]
    public void BuiltIns_IsEmpty()
    {
        // No concrete rules ship in this PR; the framework is the contract.
        Assert.Empty(SemanticValidatorRegistry.BuiltIns);
    }

    [Fact]
    public void Run_WithEmptyRegistry_ReturnsEmpty()
    {
        var diags = SemanticValidatorRegistry.Run(
            "Mac-XMSS",
            new ScriptClipModel(new FmScript([])));

        Assert.Empty(diags);
    }

    [Fact]
    public void Run_DispatchesByFormatId()
    {
        var scriptOnly = new RecordingValidator("script-only", ["Mac-XMSS"]);
        var tableOnly = new RecordingValidator("table-only", ["Mac-XMTB"]);
        var wildcard = new RecordingValidator("wildcard", [IClipSemanticValidator.AllFormats]);
        var validators = new IClipSemanticValidator[] { scriptOnly, tableOnly, wildcard };
        var script = new ScriptClipModel(new FmScript([]));

        var diags = SemanticValidatorRegistry.Run("Mac-XMSS", script, validators);

        Assert.Equal(2, diags.Count);
        Assert.True(scriptOnly.Invoked);
        Assert.False(tableOnly.Invoked);
        Assert.True(wildcard.Invoked);
    }

    [Fact]
    public void Run_SkipsValidatorsThatReturnEmpty()
    {
        var silent = new RecordingValidator("silent", [IClipSemanticValidator.AllFormats], emit: false);
        var loud = new RecordingValidator("loud", [IClipSemanticValidator.AllFormats]);

        var diags = SemanticValidatorRegistry.Run(
            "Mac-XMSS",
            new ScriptClipModel(new FmScript([])),
            [silent, loud]);

        Assert.Single(diags);
    }

    private sealed class RecordingValidator(
        string label,
        IReadOnlyCollection<string> formats,
        bool emit = true) : IClipSemanticValidator
    {
        public bool Invoked { get; private set; }
        public IReadOnlyCollection<string> FormatIds => formats;

        public IReadOnlyList<ClipParseDiagnostic> Validate(ClipModel model)
        {
            Invoked = true;
            if (!emit) return [];
            return
            [
                new ClipParseDiagnostic(
                    ParseDiagnosticKind.UnknownStep,
                    ParseDiagnosticSeverity.Info,
                    "/test",
                    label),
            ];
        }
    }
}
