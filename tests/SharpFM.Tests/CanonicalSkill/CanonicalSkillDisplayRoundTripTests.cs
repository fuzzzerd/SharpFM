using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Serialization;
using Xunit;

namespace SharpFM.Tests.CanonicalSkill;

/// <summary>
/// The display-mutation contract: every step that claims
/// <see cref="ScriptStep.IsFullyEditable"/> must survive
/// XML → POCO → display line → display parse → POCO → XML with the emitted
/// XML structurally equal to the canonical fixture. Steps whose display
/// grammar cannot carry their XML (bags, opaque option blocks) are listed in
/// <see cref="KnownDisplayDivergences"/> — the guard flips for them so the
/// list shrinks as parsers become faithful, and each entry marks a step whose
/// display edits are currently lossy despite IsFullyEditable being true.
/// </summary>
public class CanonicalSkillDisplayRoundTripTests
{
    public static IEnumerable<object[]> AllFixtures() =>
        CanonicalSkillFixtures.Names().Select(n => new object[] { n });

    [Theory]
    [MemberData(nameof(AllFixtures))]
    public void RoundTripsCanonical_ThroughDisplayText(string fixtureName)
    {
        var canonical = CanonicalSkillFixtures.Load(fixtureName);
        var step = ScriptStep.FromXml(canonical);
        if (!step.IsFullyEditable)
            return; // sealed (RawStep): display edits are anchor-preserved, not parsed

        var display = step.ToDisplayLine();
        var reparsed = ReparseDisplay(display, step);
        var roundTrips = reparsed is not null
            && StructuralXml.Equal(canonical, reparsed.ToXml(), out _);
        var why = reparsed is null
            ? "display text does not parse back to a typed step"
            : (StructuralXml.Equal(canonical, reparsed.ToXml(), out var diff) ? "" : diff);

        if (KnownDisplayDivergences.Names.Contains(fixtureName))
        {
            Assert.False(roundTrips,
                $"'{fixtureName}' now round-trips through display text — remove it from KnownDisplayDivergences.");
        }
        else
        {
            Assert.True(roundTrips,
                $"'{fixtureName}' does not survive display-text mutation: {why}\n  display: {display}");
        }
    }

    /// <summary>
    /// Parse a display line the way the editor does: split/merge via
    /// <see cref="ScriptLineParser"/>, then build the typed POCO through the
    /// step's registered display factory. A blank line (empty divider
    /// comment) parses back to an empty comment step. Returns null when the
    /// display text does not resolve to a typed step (itself a divergence).
    /// </summary>
    private static ScriptStep? ReparseDisplay(string display, ScriptStep original)
    {
        var parsed = ScriptLineParser.Parse(display);
        if (parsed.Count == 0)
            return StepDisplayFactory.TryCreate("# (comment)", original.Enabled, [""]);
        if (parsed.Count != 1) return null;

        var line = parsed[0];
        return StepDisplayFactory.TryCreate(line.StepName, !line.Disabled && original.Enabled, line.Params);
    }

    [Fact]
    public void EveryKnownDisplayDivergence_HasAFixture()
    {
        var fixtures = CanonicalSkillFixtures.Names().ToHashSet(StringComparer.Ordinal);
        var orphans = KnownDisplayDivergences.Names.Where(n => !fixtures.Contains(n)).ToList();
        Assert.True(orphans.Count == 0,
            "KnownDisplayDivergences names with no fixture: " + string.Join(", ", orphans));
    }
}
