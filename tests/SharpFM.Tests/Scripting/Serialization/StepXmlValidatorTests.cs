using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

public class StepXmlValidatorTests
{
    // Set Variable: Value, Repetition, Name (Name present and last).
    private static StepMetadata SetVarMeta() => new()
    {
        Name = "Set Variable",
        Id = 141,
        Category = "control",
        Shape =
        [
            new NamedCalcChild("Value"),
            new NamedCalcChild("Repetition"),
            new NamedTextChild("Name"),
        ],
    };

    [Fact]
    public void CanonicalOrder_NoIssues()
    {
        var step = XElement.Parse(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$x</Name></Step>");
        Assert.Empty(StepXmlValidator.Validate(step, SetVarMeta()));
    }

    [Fact]
    public void NameNotLast_IsFlagged()
    {
        // The §7.1/7.4 trap shape: Name appears before Value.
        var step = XElement.Parse(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Name>$x</Name>"
            + "<Value><Calculation><![CDATA[1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition></Step>");
        var issues = StepXmlValidator.Validate(step, SetVarMeta());
        Assert.Contains(issues, i => i.Contains("Value") && i.Contains("out of canonical order"));
    }

    [Fact]
    public void UnknownChild_IsFlagged()
    {
        var step = XElement.Parse(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$x</Name>"
            + "<Bogus/></Step>");
        var issues = StepXmlValidator.Validate(step, SetVarMeta());
        Assert.Contains(issues, i => i.Contains("Bogus") && i.Contains("not a recognized child"));
    }

    [Fact]
    public void OptionalAbsentChild_IsAllowed()
    {
        // Repetition omitted — still in order, no issue.
        var step = XElement.Parse(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[1]]></Calculation></Value>"
            + "<Name>$x</Name></Step>");
        Assert.Empty(StepXmlValidator.Validate(step, SetVarMeta()));
    }
}
