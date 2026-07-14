namespace SharpFM.Model.Parsing;

/// <summary>
/// Shared truncation for XML text/attribute values surfaced in diagnostic
/// messages or resolved snippets, so a large value doesn't blow up a message
/// or the Problems panel's detail pane.
/// </summary>
internal static class XmlTextTruncation
{
    public static string Truncate(this string s, int maxLength) =>
        s.Length <= maxLength ? s : s[..maxLength] + "…";
}
