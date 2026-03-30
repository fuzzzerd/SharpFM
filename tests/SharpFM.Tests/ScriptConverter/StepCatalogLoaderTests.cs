using System.Linq;
using SharpFM.Core.ScriptConverter;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class StepCatalogLoaderTests
{
    [Fact]
    public void LoadsCatalog_HasExpectedStepCount()
    {
        Assert.True(StepCatalogLoader.All.Count > 200);
    }

    [Fact]
    public void LookupById_Comment_ReturnsCorrectDefinition()
    {
        var step = StepCatalogLoader.ById[89];
        Assert.Equal("# (comment)", step.Name);
    }

    [Fact]
    public void LookupById_If_ReturnsCorrectDefinition()
    {
        var step = StepCatalogLoader.ById[68];
        Assert.Equal("If", step.Name);
        Assert.NotNull(step.BlockPair);
        Assert.Equal(BlockPairRole.Open, step.BlockPair!.Role);
    }

    [Fact]
    public void LookupById_SetVariable_HasCorrectParams()
    {
        var step = StepCatalogLoader.ById[141];
        Assert.Equal("Set Variable", step.Name);
        var paramNames = step.Params.Select(p => p.XmlElement).ToArray();
        Assert.Contains("Name", paramNames);
        // Value and Repetition are wrapper elements, xmlElement is "Calculation"
        var wrappers = step.Params.Select(p => p.WrapperElement).ToArray();
        Assert.Contains("Value", wrappers);
        Assert.Contains("Repetition", wrappers);
    }

    [Fact]
    public void LookupByName_CaseInsensitive()
    {
        Assert.True(StepCatalogLoader.ByName.ContainsKey("set variable"));
        Assert.True(StepCatalogLoader.ByName.ContainsKey("SET VARIABLE"));
        Assert.Equal(141, StepCatalogLoader.ByName["set variable"].Id);
    }

    [Fact]
    public void AllEntries_WithIds_HaveValidIds()
    {
        foreach (var step in StepCatalogLoader.All.Where(s => s.Id.HasValue))
        {
            Assert.True(step.Id > 0, $"Step '{step.Name}' has invalid id {step.Id}");
        }
    }

    [Fact]
    public void AllEntries_HaveNoDuplicateIds()
    {
        var ids = StepCatalogLoader.All.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToList();
        var distinct = ids.Distinct().ToList();
        Assert.Equal(distinct.Count, ids.Count);
    }

    [Fact]
    public void BlockPairSteps_HaveMatchingPartners()
    {
        var openSteps = StepCatalogLoader.All
            .Where(s => s.BlockPair?.Role == BlockPairRole.Open)
            .ToList();

        foreach (var step in openSteps)
        {
            foreach (var partner in step.BlockPair!.Partners)
            {
                Assert.True(
                    StepCatalogLoader.ByName.ContainsKey(partner),
                    $"Step '{step.Name}' has partner '{partner}' that doesn't exist in catalog");
            }
        }
    }
}
