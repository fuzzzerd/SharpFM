using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GoToPortalRowStep : ScriptStep<GoToPortalRowStep>, IStepFactory
{
    public const int XmlId = 99;
    public const string XmlName = "Go to Portal Row";

    public bool WithDialog { get; set; }
    public bool SelectAll { get; set; }
    public string Location { get; set; } = "Next";
    public bool ExitAfterLast { get; set; }
    public Calculation? Calculation { get; set; }

    /// <summary>
    /// Display edits are anchor-preserved when the dialog is suppressed —
    /// the display line never shows a "With dialog" segment, so only the
    /// default (dialog on) state survives a display round-trip.
    /// </summary>
    public override bool IsFullyEditable => WithDialog;

    // Wire bridges for the shape engine. NoInteract inverts WithDialog;
    // <Exit> is emitted (always state="True") only for Previous/Next, and the
    // bare <Calculation> only for ByCalculation — the getters return null
    // outside those locations so the Optional nodes are omitted.
    public bool NoInteract { get => !WithDialog; set => WithDialog = !value; }
    public bool? ExitWire
    {
        get => ExitAfterLast && Location is "Previous" or "Next" ? true : null;
        set => ExitAfterLast = value ?? false;
    }
    public Calculation? CalculationWire
    {
        get => Location == "ByCalculation" ? Calculation : null;
        set => Calculation = value;
    }

    private GoToPortalRowStep() : base(false) { }

    public GoToPortalRowStep(
        bool withDialog = true,
        bool selectAll = false,
        string location = "Next",
        bool exitAfterLast = false,
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        SelectAll = selectAll;
        Location = location;
        ExitAfterLast = exitAfterLast;
        Calculation = calculation;
    }

    // Hand-written: per-mode conditional token grammar (First/Last/Previous/
    // Next/ByCalculation) the shape renderer cannot produce.
    public override string ToDisplayLine()
    {
        var loc = Location == "ByCalculation" ? (Calculation?.Text ?? "") : Location;
        var parts = new System.Collections.Generic.List<string> { loc };
        if (SelectAll) parts.Add("Select");
        if (ExitAfterLast) parts.Add("Exit after last: On");
        return $"Go to Portal Row [ {string.Join(" ; ", parts)} ]";
    }

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        string location = "Next";
        bool selectAll = false, exit = false;
        Calculation? calc = null;
        bool locSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Select", StringComparison.OrdinalIgnoreCase)) selectAll = true;
            else if (t.StartsWith("Exit after last:", StringComparison.OrdinalIgnoreCase))
                exit = t.Substring(16).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (!locSeen && !string.IsNullOrWhiteSpace(t))
            {
                if (t == "First" || t == "Last" || t == "Previous" || t == "Next") location = t;
                else { location = "ByCalculation"; calc = new Calculation(t); }
                locSeen = true;
            }
        }
        WithDialog = true;
        SelectAll = selectAll;
        Location = location;
        ExitAfterLast = exit;
        Calculation = calc;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-portal-row.html",
        Shape =
        [
            new BoolStateChild("NoInteract") { HrLabel = "With dialog" },
            new BoolStateChild("SelectAll") { HrLabel = "Select" },
            new EnumValueChild("RowPageLocation") { PocoProperty = "Location", ValidValues = ["First", "Last", "Previous", "Next", "ByCalculation"], DefaultValue = "Next" },
            new BoolStateChild("Exit") { PocoProperty = "ExitWire", Optional = true, HrLabel = "Exit after last" },
            new BareCalcChild { PocoProperty = "CalculationWire", Optional = true },
        ],
    };
}
