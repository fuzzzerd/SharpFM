using System.Xml.Linq;

namespace SharpFM.Tests.CanonicalSkill;

/// <summary>
/// Loads the canonical-skill round-trip fixtures generated from the vendored
/// skill markdown (see <c>docs/filemaker-xml-canonical</c>). Each fixture is
/// one canonical <c>&lt;Step&gt;</c> element; its id and name live in the
/// element's own attributes.
/// </summary>
public static class CanonicalSkillFixtures
{
    public static string Dir =>
        Path.Combine(AppContext.BaseDirectory, "CanonicalSkill", "fixtures");

    /// <summary>Fixture base names (file name without extension), Ordinal-sorted.</summary>
    public static IEnumerable<string> Names() =>
        Directory.GetFiles(Dir, "*.xml")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .OrderBy(n => n, StringComparer.Ordinal);

    public static XElement Load(string name) =>
        XElement.Parse(File.ReadAllText(Path.Combine(Dir, name + ".xml")));
}

/// <summary>
/// Structural XML equivalence used by the canonical round-trip suite:
/// element local-names and child order must match exactly, leaf text must
/// match (CDATA and plain text compare equal via <see cref="XElement.Value"/>),
/// and attributes compare as an order-independent set. A canonical attribute
/// value of <c>"N"</c> is the skill's documented placeholder for a
/// file-specific id (script/layout/field/table) and matches any emitted value.
/// </summary>
public static class StructuralXml
{
    public static bool Equal(XElement canonical, XElement emitted, out string why)
    {
        why = "";
        if (canonical.Name.LocalName != emitted.Name.LocalName)
        {
            why = $"name {canonical.Name.LocalName} != {emitted.Name.LocalName}";
            return false;
        }

        var ca = canonical.Attributes().ToDictionary(x => x.Name.LocalName, x => x.Value);
        var ea = emitted.Attributes().ToDictionary(x => x.Name.LocalName, x => x.Value);
        foreach (var (k, v) in ca)
        {
            if (!ea.TryGetValue(k, out var ev)) { why = $"<{canonical.Name.LocalName}> missing emitted attr {k}"; return false; }
            if (v == "N") continue; // skill placeholder wildcard
            if (ev != v) { why = $"<{canonical.Name.LocalName}> attr {k}: '{v}' != '{ev}'"; return false; }
        }
        foreach (var k in ea.Keys)
            if (!ca.ContainsKey(k)) { why = $"<{canonical.Name.LocalName}> extra emitted attr {k}"; return false; }

        var cc = canonical.Elements().ToList();
        var ec = emitted.Elements().ToList();
        if (cc.Count != ec.Count)
        {
            why = $"<{canonical.Name.LocalName}> child count {cc.Count} != {ec.Count} " +
                  $"[canon: {string.Join(",", cc.Select(e => e.Name.LocalName))}] " +
                  $"[emit: {string.Join(",", ec.Select(e => e.Name.LocalName))}]";
            return false;
        }
        if (cc.Count == 0)
        {
            if (canonical.Value.Trim() != emitted.Value.Trim())
            {
                why = $"<{canonical.Name.LocalName}> text '{canonical.Value.Trim()}' != '{emitted.Value.Trim()}'";
                return false;
            }
            return true;
        }
        for (int i = 0; i < cc.Count; i++)
            if (!Equal(cc[i], ec[i], out why)) return false;
        return true;
    }
}
