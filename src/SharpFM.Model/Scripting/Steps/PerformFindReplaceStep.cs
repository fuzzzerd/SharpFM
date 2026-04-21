using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
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
    public FindReplaceOperation Operation { get; set; }
    public Calculation FindText { get; set; }
    public Calculation? ReplaceText { get; set; }

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

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            Operation.ToXml(),
            new XElement("FindCalc", FindText.ToXml("Calculation")));
        if (ReplaceText is not null)
            step.Add(new XElement("ReplaceCalc", ReplaceText.ToXml("Calculation")));
        return step;
    }

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

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        // NoInteract inverted: state="True" = With dialog: Off
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var opEl = step.Element("FindReplaceOperation");
        var op = opEl is not null ? FindReplaceOperation.FromXml(opEl) : FindReplaceOperation.Default();
        var findEl = step.Element("FindCalc")?.Element("Calculation");
        var find = findEl is not null ? Calculation.FromXml(findEl) : new Calculation("");
        var replaceEl = step.Element("ReplaceCalc")?.Element("Calculation");
        var replace = replaceEl is not null ? Calculation.FromXml(replaceEl) : null;
        return new PerformFindReplaceStep(withDialog, op, find, replace, enabled);
    }

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
