using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Steps;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Contract tests for the "sealed step" mechanism: typed POCOs are
/// always fully editable, RawSteps default to sealed (not editable)
/// unless explicitly granted via <see cref="RawStepAllowList"/>. The
/// two mechanisms must be mutually exclusive — a POCO-backed step is
/// never on the allow-list, otherwise there would be two sources of
/// truth for editability.
/// </summary>
public class RawStepAllowListTests
{
    [Fact]
    public void AllowList_IsDisjointFromRegisteredPocoNames()
    {
        // Every name on the allow-list must NOT already be backed by a
        // typed POCO — POCOs get editability from being typed, the
        // allow-list is for catalog-path steps that we've verified
        // round-trip losslessly. Double-listing would make intent unclear.
        foreach (var pocoName in StepXmlFactory.RegisteredNames)
        {
            Assert.DoesNotContain(pocoName, RawStepAllowList.Names);
        }
    }

    [Fact]
    public void TypedPocoStep_IsFullyEditable_ReturnsTrue()
    {
        var step = new IfStep(enabled: true, condition: new Calculation("$x > 0"));
        Assert.True(step.IsFullyEditable);
    }

    [Fact]
    public void TypedPocoStep_SetField_IsFullyEditable_ReturnsTrue()
    {
        var step = new SetFieldStep(
            enabled: true,
            target: FieldRef.ForField("T", 1, "F"),
            expression: new Calculation("\"x\""));
        Assert.True(step.IsFullyEditable);
    }

    [Fact]
    public void RawStep_NotInAllowList_IsFullyEditable_ReturnsFalse()
    {
        // "Allow User Abort" is a catalog-known step with no typed POCO
        // (Beep was the original canary but migrated in pilot; Halt
        // Script was the Tier-A-era canary and migrated with Tier A).
        // Not in the allow-list (which ships empty), so it must be sealed.
        var xml = XElement.Parse("<Step enable=\"True\" id=\"85\" name=\"Allow User Abort\"></Step>");
        var step = ScriptStep.FromXml(xml);

        Assert.IsType<RawStep>(step);
        Assert.False(step.IsFullyEditable);
    }

    [Fact]
    public void RawStep_UnknownStepName_IsFullyEditable_ReturnsFalse()
    {
        // Completely unknown step — not in catalog, not typed, not allowed.
        var xml = XElement.Parse("<Step enable=\"True\" id=\"9999\" name=\"FutureStep\"></Step>");
        var step = ScriptStep.FromXml(xml);

        Assert.IsType<RawStep>(step);
        Assert.False(step.IsFullyEditable);
    }
}
