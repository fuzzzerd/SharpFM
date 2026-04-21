using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

/// <summary>
/// Every step with a <c>&lt;NoInteract state="..."&gt;</c> child uses the
/// FileMaker convention where <c>state="True"</c> means "suppress the
/// dialog" = display text <c>"With dialog: Off"</c>. This test locks that
/// invariant across all NoInteract-bearing POCOs so the mapping can't
/// silently invert in any of them again.
///
/// <para>
/// The test drives FromXml → ToDisplayLine with both states and asserts
/// the expected "With dialog: On/Off" substring. Steps whose display form
/// doesn't render the flag at all (it's hidden because the step carries
/// no other user-visible state worth showing it against) are filtered.
/// </para>
/// </summary>
public class NoInteractSemanticTests
{
    [Theory]
    [InlineData("Change Password", 39)]
    [InlineData("Commit Records/Requests", 75)]
    [InlineData("Convert File", 181)]
    [InlineData("Delete All Records", 24)]
    [InlineData("Delete Portal Row", 50)]
    [InlineData("Delete Record/Request", 26)]
    [InlineData("Dial Phone", 65)]
    [InlineData("Execute SQL", 117)]
    [InlineData("Export Records", 36)]
    [InlineData("Go to Portal Row", 99)]
    [InlineData("Go to Record/Request/Page", 16)]
    [InlineData("Import Records", 35)]
    [InlineData("Insert from URL", 160)]
    [InlineData("Omit Multiple Records", 19)]
    [InlineData("Open URL", 150)]
    [InlineData("Perform Find/Replace", 128)]
    [InlineData("Print", 43)]
    [InlineData("Print Setup", 42)]
    [InlineData("Re-Login", 136)]
    [InlineData("Recover File", 32)]
    [InlineData("Relookup Field Contents", 63)]
    [InlineData("Replace Field Contents", 91)]
    [InlineData("Revert Record/Request", 19)]
    [InlineData("Save Records as Excel", 143)]
    [InlineData("Save Records as PDF", 144)]
    [InlineData("Send Mail", 63)]
    [InlineData("Sort Records", 39)]
    [InlineData("Trigger Claris Connect Flow", 0)]
    [InlineData("Truncate Table", 182)]
    public void NoInteract_True_DisplaysAsDialogOff(string stepName, int id)
    {
        var step = ParseWithNoInteract(stepName, id, state: "True");
        var display = step.ToDisplayLine();
        Assert.DoesNotContain("With dialog: On", display);
        // We allow steps whose display hides the flag entirely; when it
        // IS rendered, it must say "Off" for NoInteract=True.
        if (display.Contains("With dialog:"))
            Assert.Contains("With dialog: Off", display);
    }

    [Theory]
    [InlineData("Change Password", 39)]
    [InlineData("Commit Records/Requests", 75)]
    [InlineData("Convert File", 181)]
    [InlineData("Delete All Records", 24)]
    [InlineData("Delete Portal Row", 50)]
    [InlineData("Delete Record/Request", 26)]
    [InlineData("Dial Phone", 65)]
    [InlineData("Execute SQL", 117)]
    [InlineData("Export Records", 36)]
    [InlineData("Go to Portal Row", 99)]
    [InlineData("Go to Record/Request/Page", 16)]
    [InlineData("Import Records", 35)]
    [InlineData("Insert from URL", 160)]
    [InlineData("Omit Multiple Records", 19)]
    [InlineData("Open URL", 150)]
    [InlineData("Perform Find/Replace", 128)]
    [InlineData("Print", 43)]
    [InlineData("Print Setup", 42)]
    [InlineData("Re-Login", 136)]
    [InlineData("Recover File", 32)]
    [InlineData("Relookup Field Contents", 63)]
    [InlineData("Replace Field Contents", 91)]
    [InlineData("Revert Record/Request", 19)]
    [InlineData("Save Records as Excel", 143)]
    [InlineData("Save Records as PDF", 144)]
    [InlineData("Send Mail", 63)]
    [InlineData("Sort Records", 39)]
    [InlineData("Trigger Claris Connect Flow", 0)]
    [InlineData("Truncate Table", 182)]
    public void NoInteract_False_DisplaysAsDialogOn(string stepName, int id)
    {
        var step = ParseWithNoInteract(stepName, id, state: "False");
        var display = step.ToDisplayLine();
        Assert.DoesNotContain("With dialog: Off", display);
        if (display.Contains("With dialog:"))
            Assert.Contains("With dialog: On", display);
    }

    [Fact]
    public void EveryNoInteractStep_IsInTheory()
    {
        // Discover every POCO whose ToXml emits a <NoInteract> child and
        // confirm it's covered by the Theory data above. New POCOs with
        // NoInteract must opt in here so the inversion rule stays enforced.
        var noInteractSteps = StepRegistry.All
            .Select(m => m.Name)
            .Where(name =>
            {
                if (m(name) is not { } step) return false;
                return step.ToXml().Element("NoInteract") is not null;
            })
            .ToHashSet();

        var covered = new System.Collections.Generic.HashSet<string>
        {
            "Change Password","Commit Records/Requests","Convert File",
            "Delete All Records","Delete Portal Row","Delete Record/Request",
            "Dial Phone","Execute SQL","Export Records","Go to Portal Row",
            "Go to Record/Request/Page","Import Records","Insert from URL",
            "Omit Multiple Records","Open URL","Perform Find/Replace",
            "Print","Print Setup","Re-Login","Recover File",
            "Relookup Field Contents","Replace Field Contents",
            "Revert Record/Request","Save Records as Excel",
            "Save Records as PDF","Send Mail","Sort Records",
            "Trigger Claris Connect Flow","Truncate Table",
        };

        var missing = noInteractSteps.Except(covered).ToList();
        Assert.True(missing.Count == 0,
            "POCOs emit <NoInteract> but aren't in the Theory data above: "
            + string.Join(", ", missing));
    }

    private static SharpFM.Model.Scripting.ScriptStep ParseWithNoInteract(string name, int id, string state)
    {
        var xml = new XElement("Step",
            new XAttribute("enable", "True"),
            new XAttribute("id", id),
            new XAttribute("name", name),
            new XElement("NoInteract", new XAttribute("state", state)));
        return StepRegistry.ByName[name].FromXml!(xml);
    }

    // Helper used inside the discovery LINQ: construct the POCO from a
    // BARE <Step> (no NoInteract child). Typed POCOs always emit their
    // flag children regardless of input shape, so NoInteract appears in
    // the output. The StepChildBag-backed AI/ML POCOs echo whatever
    // children their input had — with no NoInteract in the probe input,
    // they won't emit one either, and are correctly excluded.
    private static SharpFM.Model.Scripting.ScriptStep? m(string stepName)
    {
        if (!StepRegistry.ByName.TryGetValue(stepName, out var meta)) return null;
        if (meta.FromXml is null) return null;
        var xml = new XElement("Step",
            new XAttribute("enable", "True"),
            new XAttribute("id", meta.Id),
            new XAttribute("name", stepName));
        try { return meta.FromXml(xml); }
        catch { return null; }
    }
}
