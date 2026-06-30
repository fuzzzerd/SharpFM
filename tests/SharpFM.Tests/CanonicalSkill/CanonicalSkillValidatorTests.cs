using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using Xunit;

namespace SharpFM.Tests.CanonicalSkill;

/// <summary>
/// Cross-checks every shape-driven step's canonical XML against its declared
/// <see cref="StepMetadata.Shape"/> using <see cref="StepXmlValidator"/>: each
/// child element in the skill's canonical form must be one the shape can emit,
/// in shape order. This catches a shape whose declared element order or
/// membership drifts from the canonical form even where round-trip happens to
/// pass, and exercises the validator that backs the MCP lint surface.
/// </summary>
public class CanonicalSkillValidatorTests
{
    public static IEnumerable<object[]> ShapeDrivenFixtures()
    {
        StepRegistry.Initialize();
        foreach (var name in CanonicalSkillFixtures.Names())
        {
            var step = CanonicalSkillFixtures.Load(name);
            var stepName = step.Attribute("name")?.Value ?? "";
            if (StepRegistry.ByName.TryGetValue(stepName, out var meta) && meta.Shape.Count > 0)
                yield return new object[] { name };
        }
    }

    [Theory]
    [MemberData(nameof(ShapeDrivenFixtures))]
    public void CanonicalXml_ConformsToDeclaredShape(string fixtureName)
    {
        var step = CanonicalSkillFixtures.Load(fixtureName);
        var meta = StepRegistry.ByName[step.Attribute("name")!.Value];

        var issues = StepXmlValidator.Validate(step, meta);

        Assert.True(issues.Count == 0,
            $"'{fixtureName}' violates its declared shape: {string.Join("; ", issues)}");
    }

    [Fact]
    public void ShapeDrivenSteps_AreCovered()
    {
        // Guard that the validator suite actually exercises a meaningful set of
        // shape-driven steps (rather than silently degenerating to zero).
        Assert.True(ShapeDrivenFixtures().Count() >= 20,
            "Expected the validator to cover the shape-driven steps.");
    }
}
