using System.Xml.Linq;
using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.CanonicalSkill;

/// <summary>
/// Drives every canonical-skill fixture through <see cref="ScriptStep.FromXml"/>
/// and back out through <see cref="ScriptStep.ToXml"/>, asserting the emitted
/// XML is structurally equivalent to the skill's canonical form. Fixtures known
/// to diverge today are listed in <see cref="KnownDivergences"/>; the guard
/// below flips them so the suite stays green while still flagging the moment a
/// migrated step starts round-tripping.
/// </summary>
public class CanonicalSkillRoundTripTests
{
    public static IEnumerable<object[]> AllFixtures() =>
        CanonicalSkillFixtures.Names().Select(n => new object[] { n });

    [Theory]
    [MemberData(nameof(AllFixtures))]
    public void RoundTripsCanonical_FromSkill(string fixtureName)
    {
        var canonical = CanonicalSkillFixtures.Load(fixtureName);
        var step = ScriptStep.FromXml(canonical);
        var emitted = step.ToXml();
        var roundTrips = StructuralXml.Equal(canonical, emitted, out var why);

        if (KnownDivergences.Names.Contains(fixtureName))
        {
            Assert.False(roundTrips,
                $"'{fixtureName}' now round-trips to canonical — remove it from KnownDivergences.");
        }
        else
        {
            Assert.True(roundTrips, $"'{fixtureName}' diverges from canonical: {why}");
        }
    }

    [Fact]
    public void EveryKnownDivergence_HasAFixture()
    {
        // Guard against stale entries: a name in the list with no fixture file
        // means a rename slipped through.
        var fixtures = CanonicalSkillFixtures.Names().ToHashSet(StringComparer.Ordinal);
        var orphans = KnownDivergences.Names.Where(n => !fixtures.Contains(n)).ToList();
        Assert.True(orphans.Count == 0, "KnownDivergences names with no fixture: " + string.Join(", ", orphans));
    }
}
