// Generates the canonical-skill round-trip fixtures from the vendored skill
// markdown into tests/SharpFM.Tests/CanonicalSkill/fixtures/. Run after a
// refresh (see refresh-canonical.ps1):
//
//   dotnet run --file docs/filemaker-xml-canonical/extract-fixtures.cs
//
// Each fixture file is one canonical <Step> element; the round-trip test reads
// id/name from the element's own attributes. The script also reports coverage
// against the inspector catalog (FM_STEP_IDS.js) — the 217-entry working list.

using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

var repo = FindRepoRoot();
var refDir = Path.Combine(repo, "docs", "filemaker-xml-canonical", "skill", "references");
var outDir = Path.Combine(repo, "tests", "SharpFM.Tests", "CanonicalSkill", "fixtures");
Directory.CreateDirectory(outDir);

// Step-reference files, plus core.md under a strict guard. worked-example.md
// (full snippet) and custom-functions.md (different <CustomFunction> root) are
// excluded. core.md mixes the canonical Set Variable (§3) with disabled-step
// illustrations (§4) that use multi-step fences and literal "..." placeholders,
// so for core.md we take only single-<Step> fences with a numeric id and no
// placeholder text.
var files = new (string name, bool strict)[]
{
    ("steps-control.md", false), ("steps-fields-records.md", false),
    ("steps-navigation-editing.md", false), ("steps-accounts-ai-misc.md", false),
    ("steps-windows-files.md", false), ("steps-pdf.md", false), ("steps-plugin.md", false),
    ("core.md", true),
};

var headingRx = new Regex(@"^#### (.*?) \((\d+)\)\s*$");
var fixtures = new List<(int id, string name, string xml)>();
int skippedBlocks = 0;

foreach (var (file, strict) in files)
{
    var lines = File.ReadAllLines(Path.Combine(refDir, file));
    string? curName = null;
    bool inFence = false, expectNoOptTable = false;
    var fence = new List<string>();

    foreach (var raw in lines)
    {
        var line = raw.TrimEnd();

        if (!inFence && line.StartsWith("```")) { inFence = true; fence.Clear(); continue; }
        if (inFence && line.StartsWith("```"))
        {
            inFence = false;
            ProcessFence(fence, curName, strict, ref expectNoOptTable);
            continue;
        }
        if (inFence) { fence.Add(raw); continue; }

        var hm = headingRx.Match(line);
        if (hm.Success) { curName = hm.Groups[1].Value; expectNoOptTable = false; continue; }
        if (line.StartsWith("####")) { curName = null; if (!line.Contains("No-option")) expectNoOptTable = false; continue; }

        if (expectNoOptTable && line.StartsWith("|"))
        {
            var cells = line.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length >= 2 && int.TryParse(cells[1], out var rid) &&
                !cells[0].Equals("Step", StringComparison.OrdinalIgnoreCase) && !cells[0].StartsWith("--"))
            {
                var xml = $"<Step enable=\"True\" id=\"{rid}\" name=\"{System.Security.SecurityElement.Escape(cells[0])}\"/>";
                fixtures.Add((rid, cells[0], xml));
            }
        }
    }
}

void ProcessFence(List<string> body, string? curName, bool strict, ref bool expectNoOptTable)
{
    var text = Dedent(body).Trim();
    if (!text.Contains("<Step")) return;

    XElement wrapper;
    try { wrapper = XElement.Parse("<r>" + text + "</r>", LoadOptions.PreserveWhitespace); }
    catch { skippedBlocks++; return; }

    var steps = wrapper.Elements("Step").ToList();
    if (steps.Count == 0) { skippedBlocks++; return; }

    if (strict && (steps.Count != 1 || wrapper.DescendantNodes().OfType<XText>().Any(t => t.Value.Trim() == "...")))
    {
        skippedBlocks++;
        return;
    }

    foreach (var step in steps)
    {
        var idAttr = step.Attribute("id")?.Value;
        var nameAttr = step.Attribute("name")?.Value ?? curName ?? "";
        if (idAttr is null) { skippedBlocks++; continue; }
        if (!int.TryParse(idAttr, out var id)) { expectNoOptTable = true; continue; }
        fixtures.Add((id, nameAttr, step.ToString(SaveOptions.None)));
    }
}

static string Dedent(List<string> lines)
{
    var nonEmpty = lines.Where(l => l.Trim().Length > 0).ToList();
    if (nonEmpty.Count == 0) return string.Empty;
    int min = nonEmpty.Min(l => l.Length - l.TrimStart().Length);
    return string.Join("\n", lines.Select(l => l.Length >= min ? l[min..] : l));
}

foreach (var f in Directory.GetFiles(outDir, "*.xml")) File.Delete(f);
var byId = fixtures.GroupBy(f => f.id).OrderBy(g => g.Key);
int written = 0, variants = 0;
foreach (var grp in byId)
{
    var distinct = grp.GroupBy(f => Normalize(f.xml)).Select(g => g.First()).ToList();
    for (int i = 0; i < distinct.Count; i++)
    {
        var f = distinct[i];
        var suffix = distinct.Count > 1 ? $"-{i + 1}" : "";
        var path = Path.Combine(outDir, $"{f.id:000}-{Slug(f.name)}{suffix}.xml");
        File.WriteAllText(path, f.xml.Replace("\r\n", "\n").TrimEnd() + "\n");
        written++;
        if (distinct.Count > 1) variants++;
    }
}

static string Normalize(string xml) => Regex.Replace(xml, @"\s+", " ").Trim();

static string Slug(string name)
{
    var sb = new StringBuilder();
    bool upNext = true;
    foreach (var ch in name)
    {
        if (char.IsLetterOrDigit(ch)) { sb.Append(upNext ? char.ToUpper(ch) : ch); upNext = false; }
        else upNext = true;
    }
    var s = sb.ToString();
    return s.Length == 0 ? "Step" : s;
}

var fixtureIds = fixtures.Select(f => f.id).Distinct().ToHashSet();
Console.WriteLine($"Fixtures written: {written} (across {byId.Count()} step ids, {variants} variant files)");
Console.WriteLine($"Skipped illustrative blocks: {skippedBlocks}");

var catalogJs = File.ReadAllText(Path.Combine(repo, "docs", "filemaker-xml-canonical", "inspector", "FM_STEP_IDS.js"));
var catalog = Regex.Matches(catalogJs, @"(?m)^\s*(\d+):\s*\{name:'(.*?)',cat:'(.*?)'\}")
    .Select(m => (id: int.Parse(m.Groups[1].Value), name: m.Groups[2].Value)).ToList();
var missing = catalog.Where(c => !fixtureIds.Contains(c.id)).OrderBy(c => c.id).ToList();
Console.WriteLine($"Catalog ids without a fixture ({missing.Count}): " +
    string.Join(", ", missing.Select(m => $"{m.id} {m.name}")));

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Environment.CurrentDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SharpFM.sln")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("SharpFM.sln not found above the working directory.");
}
