using System.Text.RegularExpressions;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Quote/unquote convention for names embedded in a step's human-readable
/// display line — e.g. the script name in <c>Perform Script [ "Sync" (#4) ]</c>.
/// Follows FileMaker's own calculation-string escape: a literal quote inside
/// the name is doubled (<c>"O""Brien"</c>) rather than backslash-escaped, so
/// a hand-edited display line stays unambiguous about where the name ends.
/// </summary>
public static class DisplayQuoting
{
    private static readonly Regex QuotedWithId = new(
        "^\"(?<name>.*)\"\\s*\\(#(?<id>\\d+)\\)$",
        RegexOptions.Compiled);

    /// <summary>Wraps <paramref name="name"/> in quotes, doubling any embedded quote.</summary>
    public static string Quote(string name) => $"\"{name.Replace("\"", "\"\"")}\"";

    /// <summary>
    /// Formats the lossless <c>"name" (#id)</c> form used for static
    /// references, or just <c>"name"</c> when <paramref name="id"/> is the
    /// unknown sentinel (0).
    /// </summary>
    public static string QuoteWithId(string name, int id) =>
        id == 0 ? Quote(name) : $"{Quote(name)} (#{id})";

    /// <summary>Parses a <c>"name" (#id)</c> token, undoubling escaped quotes.</summary>
    public static bool TryParseQuotedWithId(string token, out string name, out int id)
    {
        var match = QuotedWithId.Match(token);
        if (match.Success)
        {
            name = Unescape(match.Groups["name"].Value);
            id = int.Parse(match.Groups["id"].Value);
            return true;
        }

        name = "";
        id = 0;
        return false;
    }

    /// <summary>Parses a bare <c>"name"</c> token, undoubling escaped quotes.</summary>
    public static bool TryParseQuoted(string token, out string name)
    {
        if (token.Length >= 2 && token[0] == '"' && token[^1] == '"')
        {
            name = Unescape(token[1..^1]);
            return true;
        }

        name = "";
        return false;
    }

    /// <summary>
    /// Parses a <c>"name" (#id)</c> token, falling back to a bare
    /// <c>"name"</c> token with the unknown-id sentinel (0) when no id
    /// suffix is present — the shared degrade-on-hand-edit behavior used by
    /// every step that quotes a <see cref="NamedRef"/> in its display line.
    /// </summary>
    public static bool TryParseNamedRef(string token, out NamedRef namedRef)
    {
        if (TryParseQuotedWithId(token, out var name, out var id))
        {
            namedRef = new NamedRef(id, name);
            return true;
        }

        if (TryParseQuoted(token, out var bareName))
        {
            namedRef = new NamedRef(0, bareName);
            return true;
        }

        namedRef = new NamedRef(0, "");
        return false;
    }

    private static string Unescape(string s) => s.Replace("\"\"", "\"");
}
