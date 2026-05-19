using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace SharpFM.Model;

/// <summary>
/// One script extracted from a FileMaker Group paste, paired with the folder
/// path it should land in. <see cref="Xml"/> is a full <c>&lt;fmxmlsnippet&gt;</c>
/// envelope wrapping a single <c>&lt;Script&gt;</c>, ready to be re-pasted back
/// to FileMaker as a single clip.
/// </summary>
public sealed record GroupPasteEntry(string Name, string Xml, IReadOnlyList<string> FolderPath);

/// <summary>
/// Decomposed view of an <c>fmxmlsnippet</c> Group paste: the per-script
/// entries plus folder metadata captured from every enclosing
/// <c>&lt;Group&gt;</c>.
/// </summary>
public sealed record GroupPasteResult(
    IReadOnlyList<GroupPasteEntry> Entries,
    IReadOnlyList<FolderData> Folders);

/// <summary>
/// Decomposes a FileMaker script-folder paste (an <c>fmxmlsnippet</c> whose
/// root contains <c>&lt;Group&gt;</c> elements) into one entry per
/// <c>&lt;Script&gt;</c>, preserving the Group hierarchy as
/// <see cref="GroupPasteEntry.FolderPath"/> and the Group attributes as
/// <see cref="FolderData"/>.
/// </summary>
public static class GroupPasteDecomposer
{
    /// <summary>
    /// Returns the entries and folder metadata when the snippet contains any
    /// <c>&lt;Group&gt;</c> at the top level, or <c>null</c> when the snippet
    /// is a plain single-clip paste. Scripts that sit at the root alongside a
    /// Group are emitted with an empty <see cref="GroupPasteEntry.FolderPath"/>
    /// so the caller can place them at the paste root.
    /// </summary>
    public static GroupPasteResult? TryDecompose(string xml)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (XmlException)
        {
            return null;
        }

        var root = doc.Root;
        if (root is null) return null;

        var hasGroup = false;
        foreach (var _ in root.Elements("Group")) { hasGroup = true; break; }
        if (!hasGroup) return null;

        var entries = new List<GroupPasteEntry>();
        var folders = new List<FolderData>();
        Walk(root, new List<string>(), entries, folders);
        return new GroupPasteResult(entries, folders);
    }

    private static void Walk(
        XElement parent,
        List<string> folderPath,
        List<GroupPasteEntry> entries,
        List<FolderData> folders)
    {
        foreach (var child in parent.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "Script":
                    entries.Add(BuildEntry(child, folderPath));
                    break;
                case "Group":
                    var name = child.Attribute("name")?.Value ?? "Group";
                    folderPath.Add(name);
                    folders.Add(BuildFolder(child, folderPath));
                    Walk(child, folderPath, entries, folders);
                    folderPath.RemoveAt(folderPath.Count - 1);
                    break;
            }
        }
    }

    private static GroupPasteEntry BuildEntry(XElement script, List<string> folderPath)
    {
        var name = script.Attribute("name")?.Value;
        if (string.IsNullOrEmpty(name)) name = "new-clip";

        var snippet = new XElement("fmxmlsnippet",
            new XAttribute("type", "FMObjectList"),
            new XElement(script));

        var xml = new XDocument(snippet).ToString(SaveOptions.DisableFormatting);

        return new GroupPasteEntry(name, xml, folderPath.ToArray());
    }

    private static FolderData BuildFolder(XElement group, List<string> folderPath)
    {
        int? id = null;
        if (int.TryParse(group.Attribute("id")?.Value, out var parsedId))
        {
            id = parsedId;
        }

        return new FolderData(folderPath.ToArray())
        {
            Id = id,
            IncludeInMenu = ParseFmBool(group.Attribute("includeInMenu"), defaultValue: true),
            GroupCollapsed = ParseFmBool(group.Attribute("groupCollapsed"), defaultValue: false),
        };
    }

    private static bool ParseFmBool(XAttribute? attribute, bool defaultValue)
    {
        if (attribute is null) return defaultValue;
        return string.Equals(attribute.Value, "True", System.StringComparison.OrdinalIgnoreCase);
    }
}
