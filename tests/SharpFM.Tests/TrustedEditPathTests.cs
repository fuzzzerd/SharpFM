using System.Linq;
using SharpFM.Model;
using SharpFM.Model.Parsing;
using SharpFM.Model.Schema;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests;

/// <summary>
/// Trusted-edit path: <see cref="Clip.FromEditor"/> bypasses the strategy
/// parse + round-trip diff because the editor's model is the source of
/// truth. Diagnostics that don't depend on structural diff (e.g. RawStep
/// inventory) are still synthesised so the UI signal stays consistent
/// with the strategy-driven path.
/// </summary>
public class TrustedEditPathTests
{
    [Fact]
    public void FromEditor_ProducesParseSuccessWithProvidedModel()
    {
        var script = new FmScript([]);
        var model = new ScriptClipModel(script);

        var clip = Clip.FromEditor("X", "Mac-XMSS",
            "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>", model);

        var success = Assert.IsType<ParseSuccess>(clip.Parsed);
        Assert.Same(model, success.Model);
    }

    [Fact]
    public void FromEditor_LosslessForCleanScript()
    {
        var script = new FmScript(
        [
            new SetVariableStep(true, "$x", new SharpFM.Model.Scripting.Values.Calculation("1")),
        ]);
        var model = new ScriptClipModel(script);

        var clip = Clip.FromEditor("X", "Mac-XMSS",
            "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>", model);

        Assert.True(clip.Parsed.Report.IsLossless);
    }

    [Fact]
    public void FromEditor_SurfacesRawStepAsUnknownStep()
    {
        var rawStepXml = System.Xml.Linq.XElement.Parse(
            "<Step enable=\"True\" id=\"99999\" name=\"FutureStep\"/>");
        var script = new FmScript([new RawStep(rawStepXml)]);
        var model = new ScriptClipModel(script);

        var clip = Clip.FromEditor("X", "Mac-XMSS",
            "<fmxmlsnippet type=\"FMObjectList\"/>", model);

        var success = Assert.IsType<ParseSuccess>(clip.Parsed);
        var diagnostic = Assert.Single(success.Report.Diagnostics);
        Assert.Equal(ParseDiagnosticKind.UnknownStep, diagnostic.Kind);
        Assert.Equal(ParseDiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("FutureStep", diagnostic.Message);
    }

    [Fact]
    public void FromEditor_TableModel_LosslessReport()
    {
        var table = new FmTable("People");
        var model = new TableClipModel(table);

        var clip = Clip.FromEditor("X", "Mac-XMTB",
            "<fmxmlsnippet type=\"FMObjectList\"><BaseTable name=\"People\"/></fmxmlsnippet>", model);

        Assert.True(clip.Parsed.Report.IsLossless);
    }

    [Fact]
    public void FromEditor_OpaqueModel_LosslessReport()
    {
        var model = new OpaqueClipModel("<root/>");

        var clip = Clip.FromEditor("X", "Mac-XML2", "<root/>", model);

        Assert.True(clip.Parsed.Report.IsLossless);
    }

    [Fact]
    public void FromEditor_DoesNotInvokeStrategy_ForMalformedXml()
    {
        // Strategy.Parse on this would return ParseFailure (no <fmxmlsnippet>
        // root). FromEditor trusts the model + xml regardless of strategy
        // opinions about the XML — the editor produced both, they're
        // self-consistent by construction.
        var script = new FmScript([]);
        var model = new ScriptClipModel(script);

        var clip = Clip.FromEditor("X", "Mac-XMSS", "<not-fmxmlsnippet/>", model);

        Assert.IsType<ParseSuccess>(clip.Parsed);
    }

    [Fact]
    public void FromEditor_LargeScriptDoesNotInvokeRoundTripDiff()
    {
        // Sanity: 500 steps would burn cycles on the strategy path. The
        // trusted-edit path completes without scanning the produced XML
        // (we don't pass the strategy here at all).
        var steps = Enumerable.Range(0, 500)
            .Select(i => (ScriptStep)new SetVariableStep(
                true, $"$v{i}", new SharpFM.Model.Scripting.Values.Calculation(i.ToString())))
            .ToList();
        var script = new FmScript(steps);
        var model = new ScriptClipModel(script);

        var clip = Clip.FromEditor("X", "Mac-XMSS",
            "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>", model);

        Assert.True(clip.Parsed.Report.IsLossless);
        Assert.Same(model, ((ParseSuccess)clip.Parsed).Model);
    }
}
