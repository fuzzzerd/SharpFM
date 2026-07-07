using System.Text;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Steps;
using Xunit;
using Xunit.Abstractions;

namespace SharpFM.Tests.CanonicalSkill;

// TEMPORARY audit for the display-rendering cutover: compares every
// fixture-parsed step's hand-written ToDisplayLine() (the shipped truth)
// against the shape-driven StepDisplayRenderer. Buckets the results so the
// cutover work-list is exact. Deleted when the cutover completes.
public class TempDisplayRendererParityAudit
{
    private readonly ITestOutputHelper _output;
    public TempDisplayRendererParityAudit(ITestOutputHelper output) => _output = output;

    [Fact]
    public void DumpMismatches()
    {
        StepRegistry.Initialize();
        var identical = new List<string>();
        var different = new StringBuilder();
        int diffCount = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in CanonicalSkillFixtures.Names())
        {
            var step = ScriptStep.FromXml(CanonicalSkillFixtures.Load(name));
            if (step is RawStep) continue;
            var meta = StepRegistry.MetadataFor(step);
            if (meta is null) continue;

            var hand = step.ToDisplayLine();
            var shape = StepDisplayRenderer.Render(step, meta);
            var key = $"{meta.Name}|{hand}|{shape}";
            if (!seen.Add(key)) continue;

            if (hand == shape)
            {
                identical.Add(name);
            }
            else
            {
                diffCount++;
                different.AppendLine($"== {name} ({meta.Name})");
                different.AppendLine($"   hand:  {hand}");
                different.AppendLine($"   shape: {shape}");
            }
        }

        _output.WriteLine($"IDENTICAL: {identical.Count}   DIFFERENT: {diffCount}");
        _output.WriteLine(different.ToString());
        Assert.True(true);
    }
}
