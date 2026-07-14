using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SharpFM.Model.Parsing;

/// <summary>
/// Resolves a <see cref="ClipParseDiagnostic.Location"/> path back to the XML
/// it points at, so a diagnostic can show its actual context. Walks the same
/// <c>/Root/Child[n]/@Attr</c> grammar <see cref="XmlRoundTripDiff"/> emits
/// against a freshly parsed copy of the clip's current XML.
/// </summary>
public static class ClipParseLocationResolver
{
    private const string NotFound = "(not found in current XML)";
    private const int MaxLength = 300;

    private static readonly Regex IndexedSegment = new(
        @"^(?<name>[^\[\]/@]+)\[(?<index>\d+)\]$",
        RegexOptions.Compiled);

    /// <summary>
    /// Resolve <paramref name="location"/> against <paramref name="rawXml"/>.
    /// Never throws: malformed XML, a path whose root name doesn't match, or a
    /// segment that no longer resolves (e.g. it named something only present
    /// in the round-tripped output, never in the source XML this walks) all
    /// fall back to a short placeholder.
    /// </summary>
    public static string Resolve(string rawXml, string location)
    {
        XElement root;
        try
        {
            root = XElement.Parse(rawXml);
        }
        catch (Exception)
        {
            return NotFound;
        }

        var segments = location.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return root.ToString().Truncate(MaxLength);
        }

        if (segments[0] != root.Name.LocalName)
        {
            return NotFound;
        }

        var current = root;
        for (var i = 1; i < segments.Length; i++)
        {
            var segment = segments[i];

            if (segment.StartsWith('@'))
            {
                var attr = current.Attribute(segment[1..]);
                return attr is null ? NotFound : $"{attr.Name.LocalName}=\"{attr.Value}\"".Truncate(MaxLength);
            }

            var match = IndexedSegment.Match(segment);
            if (!match.Success)
            {
                return NotFound;
            }

            var name = match.Groups["name"].Value;
            var index = int.Parse(match.Groups["index"].Value);
            var candidate = current.Elements()
                .Where(e => e.Name.LocalName == name)
                .Skip(index - 1)
                .FirstOrDefault();

            if (candidate is null)
            {
                return NotFound;
            }

            current = candidate;
        }

        return current.ToString().Truncate(MaxLength);
    }
}
