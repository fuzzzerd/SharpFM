using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Parsing;

/// <summary>
/// Structural diff between two XML trees, used by clip-type strategies as the
/// "did this round-trip cleanly?" check. Compares <c>parsed-then-serialized</c>
/// against the source XML and reports anything the round trip dropped or
/// changed as <see cref="ClipParseDiagnostic"/>s.
/// </summary>
/// <remarks>
/// The comparison is name-based and order-insensitive within a parent (an
/// out-of-order rewrite of children is not flagged as a loss); attribute order
/// is ignored; element-text is compared trimmed (whitespace-only differences
/// are noise from pretty-printing). Differences are categorised by parent
/// element so consumers see <see cref="ParseDiagnosticKind.UnknownStepElement"/>
/// vs. <see cref="ParseDiagnosticKind.UnknownClipElement"/> rather than a flat
/// "something differs" stream.
/// </remarks>
public static class XmlRoundTripDiff
{
    /// <summary>
    /// Compare <paramref name="input"/> (source XML) against <paramref name="output"/>
    /// (model serialised back to XML). Anything in input absent from output is a
    /// loss; anything in output absent from input is reported as informational
    /// (typically a default the model emits and the input omitted).
    /// </summary>
    public static IReadOnlyList<ClipParseDiagnostic> Compute(XElement input, XElement output)
    {
        var diagnostics = new List<ClipParseDiagnostic>();
        CompareElements(input, output, "/" + input.Name.LocalName, diagnostics);
        DiffNamespaces(input, output, diagnostics);
        return diagnostics;
    }

    private static void CompareElements(XElement input, XElement output, string path, List<ClipParseDiagnostic> diags)
    {
        DiffAttributes(input, output, path, diags);

        var inputHasChildren = input.HasElements;
        var outputHasChildren = output.HasElements;

        if (!inputHasChildren && !outputHasChildren)
        {
            var inputText = input.Value.Trim();
            var outputText = output.Value.Trim();
            if (inputText != outputText)
            {
                diags.Add(new ClipParseDiagnostic(
                    ParseDiagnosticKind.RoundTripValueMismatch,
                    ParseDiagnosticSeverity.Warning,
                    path,
                    $"text differs: input '{Truncate(inputText)}' vs output '{Truncate(outputText)}'"));
            }
            return;
        }

        var outputByName = output.Elements()
            .GroupBy(e => e.Name.LocalName)
            .ToDictionary(g => g.Key, g => new Queue<XElement>(g));

        var indexByName = new Dictionary<string, int>();
        foreach (var inputChild in input.Elements())
        {
            var name = inputChild.Name.LocalName;
            indexByName[name] = indexByName.TryGetValue(name, out var prior) ? prior + 1 : 1;
            var childPath = $"{path}/{name}[{indexByName[name]}]";

            if (outputByName.TryGetValue(name, out var queue) && queue.Count > 0)
            {
                CompareElements(inputChild, queue.Dequeue(), childPath, diags);
            }
            else
            {
                diags.Add(new ClipParseDiagnostic(
                    KindForUnmodeledChild(input.Name.LocalName),
                    ParseDiagnosticSeverity.Warning,
                    childPath,
                    $"input child <{name}> not preserved through round trip"));
            }
        }

        foreach (var (_, leftover) in outputByName)
        {
            foreach (var orphan in leftover)
            {
                diags.Add(new ClipParseDiagnostic(
                    ParseDiagnosticKind.RoundTripValueMismatch,
                    ParseDiagnosticSeverity.Info,
                    $"{path}/{orphan.Name.LocalName}",
                    $"output emitted <{orphan.Name.LocalName}> not present in input (likely a default)"));
            }
        }
    }

    private static void DiffAttributes(XElement input, XElement output, string path, List<ClipParseDiagnostic> diags)
    {
        var outputAttrs = output.Attributes()
            .Where(a => !a.IsNamespaceDeclaration)
            .ToDictionary(a => a.Name.LocalName, a => a.Value);

        foreach (var inputAttr in input.Attributes())
        {
            if (inputAttr.IsNamespaceDeclaration)
            {
                continue;
            }

            var name = inputAttr.Name.LocalName;
            if (!outputAttrs.TryGetValue(name, out var outputValue))
            {
                diags.Add(new ClipParseDiagnostic(
                    KindForUnmodeledAttribute(input.Name.LocalName),
                    ParseDiagnosticSeverity.Warning,
                    $"{path}/@{name}",
                    $"input attribute @{name}=\"{Truncate(inputAttr.Value)}\" not preserved through round trip"));
            }
            else if (outputValue != inputAttr.Value)
            {
                diags.Add(new ClipParseDiagnostic(
                    ParseDiagnosticKind.RoundTripValueMismatch,
                    ParseDiagnosticSeverity.Warning,
                    $"{path}/@{name}",
                    $"attribute @{name}: input \"{Truncate(inputAttr.Value)}\" vs output \"{Truncate(outputValue)}\""));
            }
        }
    }

    private static void DiffNamespaces(XElement input, XElement output, List<ClipParseDiagnostic> diags)
    {
        var inputNamespaces = CollectNamespaces(input);
        var outputNamespaces = CollectNamespaces(output);
        foreach (var ns in inputNamespaces.Except(outputNamespaces))
        {
            diags.Add(new ClipParseDiagnostic(
                ParseDiagnosticKind.DroppedNamespace,
                ParseDiagnosticSeverity.Warning,
                "/",
                $"namespace \"{ns}\" declared in input was not preserved through round trip"));
        }
    }

    private static HashSet<string> CollectNamespaces(XElement root)
    {
        var set = new HashSet<string>();
        foreach (var element in root.DescendantsAndSelf())
        {
            foreach (var attr in element.Attributes())
            {
                if (attr.IsNamespaceDeclaration)
                {
                    set.Add(attr.Value);
                }
            }
        }
        return set;
    }

    private static ParseDiagnosticKind KindForUnmodeledChild(string parentLocalName) =>
        parentLocalName == "Step"
            ? ParseDiagnosticKind.UnknownStepElement
            : ParseDiagnosticKind.UnknownClipElement;

    private static ParseDiagnosticKind KindForUnmodeledAttribute(string parentLocalName) =>
        parentLocalName == "Step"
            ? ParseDiagnosticKind.UnknownStepAttribute
            : ParseDiagnosticKind.UnknownClipAttribute;

    private const int TruncateLength = 60;

    private static string Truncate(string s) =>
        s.Length <= TruncateLength ? s : s[..TruncateLength] + "…";
}
