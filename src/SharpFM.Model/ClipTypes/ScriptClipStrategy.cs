using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SharpFM.Model.Parsing;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Steps;

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
        if (string.IsNullOrWhiteSpace(xml))
        {
            return Failure(ParseDiagnosticKind.XmlMalformed, "/", "input was empty", "empty xml");
        }

        XElement input;
        try
        {
            input = XElement.Parse(xml);
        }
        catch (XmlException ex)
        {
            return Failure(ParseDiagnosticKind.XmlMalformed, "/", ex.Message, "malformed xml");
        }

        if (input.Name.LocalName != "fmxmlsnippet")
        {
            return Failure(
                ParseDiagnosticKind.UnsupportedClipType,
                "/" + input.Name.LocalName,
                $"expected <fmxmlsnippet>, found <{input.Name.LocalName}>",
                "unsupported root element");
        }

        FmScript script;
        try
        {
            script = FmScript.FromXml(xml);
        }
        catch (Exception ex)
        {
            return Failure(ParseDiagnosticKind.XmlMalformed, "/", ex.Message, "failed to parse script");
        }

        var output = XElement.Parse(script.ToXml());
        var diagnostics = new List<ClipParseDiagnostic>(XmlRoundTripDiff.Compute(input, output));

        var rawStepIndex = 0;
        foreach (var step in script.Steps)
        {
            rawStepIndex++;
            if (step is RawStep raw)
            {
                var stepName = raw.Element.Attribute("name")?.Value ?? "Unknown";
                diagnostics.Add(new ClipParseDiagnostic(
                    ParseDiagnosticKind.UnknownStep,
                    ParseDiagnosticSeverity.Info,
                    $"/fmxmlsnippet/Step[{rawStepIndex}]",
                    $"step '{stepName}' is not modeled by the host; preserved verbatim as RawStep"));
            }
        }

        var report = diagnostics.Count == 0
            ? ClipParseReport.Empty
            : new ClipParseReport(diagnostics);

        return new ParseSuccess(new ScriptClipModel(script), report);
    }

    public string DefaultXml(string clipName) =>
        "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>";

    private static ParseFailure Failure(
        ParseDiagnosticKind kind, string location, string message, string reason)
    {
        return new ParseFailure(reason, new ClipParseReport(
        [
            new ClipParseDiagnostic(kind, ParseDiagnosticSeverity.Error, location, message),
        ]));
    }
}
