using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Perform Find/Replace. Carries a typed <see cref="FindReplaceOperation"/>
/// record with the full set of operation attributes, plus a find calc and
/// an optional replace calc wrapped in FindCalc / ReplaceCalc elements.
/// </summary>
public sealed class PerformFindReplaceStep : ScriptStep, IStepFactory
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<PerformFindReplaceStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Display is lossy — operation flags like MatchCase can't be
        // expressed in this short form.
        bool withDialog = true;
        Calculation find = new("");
        Calculation? replace = null;
        int positional = 0;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (!string.IsNullOrWhiteSpace(t))
            {
                if (positional == 0) { find = new Calculation(t); positional++; }
                else if (positional == 1) { replace = new Calculation(t); positional++; }
            }
        }
        return new PerformFindReplaceStep(withDialog, FindReplaceOperation.Default(), find, replace, enabled);
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
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Hidden },
            new ValueTypeChild("FindReplaceOperation") { PocoProperty = "Operation", Display = DisplayMode.Hidden },
            new NamedCalcChild("FindCalc") { PocoProperty = "FindText", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("ReplaceCalc") { PocoProperty = "ReplaceText", Optional = true, Display = DisplayMode.Native },
        ],
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "FindReplaceOperation", XmlElement = "FindReplaceOperation", Type = "complex", Required = true },
            new ParamMetadata { Name = "FindCalc", XmlElement = "Calculation", Type = "namedCalc", Required = true },
            new ParamMetadata { Name = "ReplaceCalc", XmlElement = "Calculation", Type = "namedCalc" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
