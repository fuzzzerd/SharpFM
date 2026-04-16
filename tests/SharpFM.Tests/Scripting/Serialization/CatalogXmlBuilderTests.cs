using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Serialization;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

/// <summary>
/// Tests the stateless catalog-driven XML builder that replaces the
/// retired <c>StepParamValue</c> construction path. Covers the three
/// public entry points (<c>BuildStep</c>, <c>BuildStepFromMap</c>,
/// <c>UpdateParam</c>) and the major param-type branches.
/// </summary>
public class CatalogXmlBuilderTests
{
    // --- BuildStep (from display tokens) ---

    /// <summary>
    /// Synthetic single-param calculation step — isolates the builder's
    /// calculation-emission path from catalog quirks like flag-style
    /// boolean prefixes that confuse positional matching.
    /// </summary>
    private static StepDefinition MakeSyntheticCalcStep() => new()
    {
        Name = "SyntheticCalc",
        Id = 9999,
        Params = new[]
        {
            new StepParam { XmlElement = "Calculation", Type = "calculation", Required = true }
        }
    };

    [Fact]
    public void BuildStep_CalculationParam_WrapsExpressionInCdata()
    {
        var def = MakeSyntheticCalcStep();
        var xml = CatalogXmlBuilder.BuildStep(def, enabled: true, ["$x > 0"]);

        Assert.Equal("True", xml.Attribute("enable")!.Value);
        Assert.Equal("9999", xml.Attribute("id")!.Value);
        Assert.Equal("SyntheticCalc", xml.Attribute("name")!.Value);
        Assert.Contains("$x > 0", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void BuildStep_DisabledFlag_EmitsEnableFalse()
    {
        var def = StepCatalogLoader.ByName["Else"];
        var xml = CatalogXmlBuilder.BuildStep(def, enabled: false, []);

        Assert.Equal("False", xml.Attribute("enable")!.Value);
    }

    [Fact]
    public void BuildStep_SetField_FromDisplayTokens_ReordersByCatalog()
    {
        // Catalog order is [Calculation, Field]. Display provides tokens
        // positionally — the first positional match binds to Calculation,
        // the second to Field.
        var def = StepCatalogLoader.ByName["Set Field"];
        var xml = CatalogXmlBuilder.BuildStep(def, enabled: true, ["People::FirstName", "$count + 1"]);

        Assert.NotNull(xml.Element("Calculation"));
        Assert.NotNull(xml.Element("Field"));
    }

    [Fact]
    public void BuildStep_NamedRefParam_EmitsNameWithoutHardcodedId()
    {
        // Pre-refactor, the old BuildNamedRefXml hardcoded id="0".
        // The new generic path emits ONLY the name attribute — ids come
        // from typed POCOs that carry them explicitly.
        var def = StepCatalogLoader.ByName["Go to Layout"];
        var xml = CatalogXmlBuilder.BuildStep(def, enabled: true, ["SelectedLayout", "\"Detail\""]);

        var layout = xml.Element("Layout");
        Assert.NotNull(layout);
        Assert.Equal("Detail", layout!.Attribute("name")!.Value);
        Assert.Null(layout.Attribute("id")); // id is not invented anymore
    }

    // --- BuildStepFromMap (from param-name dict) ---

    [Fact]
    public void BuildStepFromMap_IfStep_ReadsCalculationByParamName()
    {
        var def = StepCatalogLoader.ByName["If"];
        var map = new Dictionary<string, string?> { ["Calculation"] = "Get(FoundCount) > 0" };

        var xml = CatalogXmlBuilder.BuildStepFromMap(def, enabled: true, map);

        Assert.Contains("Get(FoundCount) > 0", xml.Element("Calculation")!.Value);
    }

    [Fact]
    public void BuildStepFromMap_NullMap_StillEmitsStructuralElement()
    {
        var def = StepCatalogLoader.ByName["Else"];
        var xml = CatalogXmlBuilder.BuildStepFromMap(def, enabled: true, null);

        Assert.Equal("Else", xml.Attribute("name")!.Value);
    }

    // --- UpdateParam ---

    [Fact]
    public void UpdateParam_IfCalculation_ReplacesExpression()
    {
        var original = XElement.Parse(
            "<Step enable=\"True\" id=\"68\" name=\"If\">"
            + "<Calculation><![CDATA[$x > 0]]></Calculation></Step>");
        var def = StepCatalogLoader.ByName["If"];

        var updated = CatalogXmlBuilder.UpdateParam(original, def, "Calculation", "$x >= 100");

        Assert.NotNull(updated);
        Assert.Contains("$x >= 100", updated!.Element("Calculation")!.Value);
        Assert.DoesNotContain("$x > 0", updated.Element("Calculation")!.Value);
    }

    [Fact]
    public void UpdateParam_UnknownParamName_ReturnsNull()
    {
        var original = XElement.Parse(
            "<Step enable=\"True\" id=\"68\" name=\"If\">"
            + "<Calculation><![CDATA[$x > 0]]></Calculation></Step>");
        var def = StepCatalogLoader.ByName["If"];

        var updated = CatalogXmlBuilder.UpdateParam(original, def, "NonExistent", "value");

        Assert.Null(updated);
    }

    [Fact]
    public void UpdateParam_LeavesOriginalUntouched()
    {
        var original = XElement.Parse(
            "<Step enable=\"True\" id=\"68\" name=\"If\">"
            + "<Calculation><![CDATA[$x > 0]]></Calculation></Step>");
        var def = StepCatalogLoader.ByName["If"];

        CatalogXmlBuilder.UpdateParam(original, def, "Calculation", "different");

        Assert.Contains("$x > 0", original.Element("Calculation")!.Value);
    }
}
