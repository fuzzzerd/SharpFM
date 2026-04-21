using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// A FileMaker file or ODBC data source reference used by Open File and
/// Close File. Shape: <c>&lt;FileReference id="0" name=""&gt;&lt;UniversalPathList&gt;file:Name&lt;/UniversalPathList&gt;&lt;/FileReference&gt;</c>.
///
/// All three components — id, name, and the path list — round-trip
/// losslessly. The path list is a semicolon-delimited platform-agnostic
/// list (e.g. <c>file:Foo;filewin:C:/.../Foo.fmp12</c>); we preserve it
/// as an opaque string since FM's compositing of it is version-specific
/// and not documented.
/// </summary>
public sealed record FileReference(int Id, string Name, string Paths)
{
    public XElement ToXml(string elementName = "FileReference") =>
        new(elementName,
            new XAttribute("id", Id),
            new XAttribute("name", Name),
            new XElement("UniversalPathList", Paths));

    public static FileReference FromXml(XElement element)
    {
        var idStr = element.Attribute("id")?.Value;
        var id = int.TryParse(idStr, out var parsed) ? parsed : 0;
        var name = element.Attribute("name")?.Value ?? "";
        var paths = element.Element("UniversalPathList")?.Value ?? "";
        return new FileReference(id, name, paths);
    }

    /// <summary>
    /// Display rendering: <c>"Name"</c> when the name is non-empty, otherwise
    /// the first path in the UniversalPathList. Matches FM Pro's rendering
    /// which prefers the human-readable file name.
    /// </summary>
    public string ToDisplayString()
    {
        if (!string.IsNullOrEmpty(Name)) return $"\"{Name}\"";
        return string.IsNullOrEmpty(Paths) ? "\"\"" : $"\"{Paths}\"";
    }

    public static FileReference FromDisplayToken(string token)
    {
        var t = token.Trim();
        if (t.StartsWith("\"") && t.EndsWith("\"") && t.Length >= 2)
            t = t.Substring(1, t.Length - 2);
        return new FileReference(0, t, "");
    }
}
