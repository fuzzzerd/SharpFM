using System;
using System.Collections.Generic;
using SharpFM.Model.Parsing;
using SharpFM.Model.Scripting;

namespace SharpFM.Model.ClipTypes;

/// <summary>
/// Strategy for the two FileMaker script clipboard formats. <c>Mac-XMSS</c>
/// (Script Steps) holds a bare list of steps under <c>&lt;fmxmlsnippet&gt;</c>;
/// <c>Mac-XMSC</c> (Script) wraps the steps in a <c>&lt;Script&gt;</c> element
/// carrying name/id/run-fullaccess metadata. <see cref="FmScript.FromXml"/>
/// already handles both shapes — this strategy adds the round-trip diff and
/// the unknown-step inventory to the result.
/// </summary>
public sealed class ScriptClipStrategy : IClipTypeStrategy
{
    /// <summary>Strategy instance for <c>Mac-XMSS</c> (Script Steps).</summary>
    public static IClipTypeStrategy Steps { get; } =
        new ScriptClipStrategy("Mac-XMSS", "Script Steps");

    /// <summary>Strategy instance for <c>Mac-XMSC</c> (Script).</summary>
    public static IClipTypeStrategy Script { get; } =
        new ScriptClipStrategy("Mac-XMSC", "Script");

    private ScriptClipStrategy(string formatId, string displayName)
    {
        FormatId = formatId;
        DisplayName = displayName;
    }

    public string FormatId { get; }
    public string DisplayName { get; }

    public ClipParseResult Parse(string xml)
    {
        if (!ClipStrategyHelpers.TryParseFmxmlsnippet(xml, out var input, out var failure))
        {
            return failure;
        }

        FmScript script;
        try
        {
            script = FmScript.FromXml(xml);
        }
        catch (Exception ex)
        {
            return ClipStrategyHelpers.Failure(
                ParseDiagnosticKind.XmlMalformed, "/", ex.Message, "failed to parse script");
        }

        var diagnostics = new List<ClipParseDiagnostic>(
            XmlRoundTripDiff.Compute(input, script.ToElement()));
        diagnostics.AddRange(ClipStrategyHelpers.RawStepDiagnostics(script));

        var report = diagnostics.Count == 0
            ? ClipParseReport.Empty
            : new ClipParseReport(diagnostics);

        return new ParseSuccess(new ScriptClipModel(script), report);
    }

    public string DefaultXml(string clipName) =>
        "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>";
}
