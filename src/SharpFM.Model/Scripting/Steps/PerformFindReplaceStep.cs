using System;
using System.Collections.Generic;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Perform Find/Replace. Carries a typed <see cref="FindReplaceOperation"/>
/// record with the full set of operation attributes, plus a find calc and
/// an optional replace calc wrapped in FindCalc / ReplaceCalc elements.
/// </summary>
public sealed class PerformFindReplaceStep : ScriptStep<PerformFindReplaceStep>, IStepFactory
{
    public const int XmlId = 128;
    public const string XmlName = "Perform Find/Replace";

    public bool WithDialog { get; set; }
    public FindReplaceOperation Operation { get; set; } = FindReplaceOperation.Default();
    public Calculation FindText { get; set; } = new("");
    public Calculation? ReplaceText { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private PerformFindReplaceStep() : base(false) { }

    public PerformFindReplaceStep(
        bool withDialog = true,
        FindReplaceOperation? operation = null,
        Calculation? findText = null,
        Calculation? replaceText = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Operation = operation ?? FindReplaceOperation.Default();
        FindText = findText ?? new Calculation("");
        ReplaceText = replaceText;
    }

    // Hand-written: the operation type lives inside a ValueTypeChild and the
    // replace calc is a conditional positional token — variant grammar the
    // shape renderer cannot express.
    public override string ToDisplayLine()
    {
        var opDisplay = Operation.Type switch
        {
            "FindNext" => "Find Next",
            "ReplaceAndFind" => "Replace and Find",
            "Replace" => "Replace",
            "ReplaceAll" => "Replace All",
            _ => Operation.Type,
        };
        var parts = new System.Collections.Generic.List<string>
        {
            $"With dialog: {(WithDialog ? "On" : "Off")}",
            FindText.Text,
        };
        if (ReplaceText is not null) parts.Add(ReplaceText.Text);
        parts.Add(opDisplay);
        return $"Perform Find/Replace [ {string.Join(" ; ", parts)} ]";
    }

    /// <summary>
    /// Display edits are anchor-preserved when operation state the display
    /// line cannot carry is present: match flags, a non-forward direction,
    /// or non-default within/across scopes (the display shows only the
    /// operation type).
    /// </summary>
    public override bool IsFullyEditable =>
        Operation is { MatchWholeWords: false, MatchCase: false, Direction: "Forward", WithinOptions: "All", AcrossOptions: "All" };

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        // Display grammar: [ With dialog: On/Off ; find ; replace? ; operation ].
        // The last positional token is the operation type; earlier positional
        // tokens are the find and optional replace calcs.
        bool withDialog = true;
        var positional = new List<string>();
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else
                positional.Add(t);
        }

        var opType = "FindNext";
        if (positional.Count > 0)
        {
            opType = positional[^1] switch
            {
                "Find Next" => "FindNext",
                "Replace and Find" => "ReplaceAndFind",
                "Replace" => "Replace",
                "Replace All" => "ReplaceAll",
                var other => other,
            };
            positional.RemoveAt(positional.Count - 1);
        }

        Calculation find = positional.Count > 0 ? new(positional[0]) : new("");
        Calculation? replace = positional.Count > 1 ? new(positional[1]) : null;
        // Canonical unconfigured operation flags; configured flags are sealed state.
        var operation = new FindReplaceOperation(opType, "Forward", false, false, "All", "All");
        WithDialog = withDialog;
        Operation = operation;
        FindText = find;
        ReplaceText = replace;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "editing",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-find-replace.html",
        // Canonical: NoInteract (inverts WithDialog) then FindReplaceOperation;
        // the FindCalc/ReplaceCalc wrappers are omitted when empty (Optional).
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Native },
            new ValueTypeChild("FindReplaceOperation") { PocoProperty = "Operation", Display = DisplayMode.Native },
            new NamedCalcChild("FindCalc") { PocoProperty = "FindText", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("ReplaceCalc") { PocoProperty = "ReplaceText", Optional = true, Display = DisplayMode.Native },
        ],
    };
}
