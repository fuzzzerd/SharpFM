using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GoToPortalRowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 99;
    public const string XmlName = "Go to Portal Row";

    public bool WithDialog { get; set; }
    public bool SelectAll { get; set; }
    public string Location { get; set; }
    public bool ExitAfterLast { get; set; }
    public Calculation? Calculation { get; set; }

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

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("SelectAll", new XAttribute("state", SelectAll ? "True" : "False")),
            new XElement("RowPageLocation", new XAttribute("value", Location)));
        if (ExitAfterLast && (Location == "Previous" || Location == "Next"))
            step.Add(new XElement("Exit", new XAttribute("state", "True")));
        if (Location == "ByCalculation" && Calculation is not null)
            step.Add(Calculation.ToXml("Calculation"));
        return step;
    }

    public override string ToDisplayLine()
    {
        var loc = Location == "ByCalculation" ? (Calculation?.Text ?? "") : Location;
        var parts = new System.Collections.Generic.List<string> { loc };
        if (SelectAll) parts.Add("Select");
        if (ExitAfterLast) parts.Add("Exit after last: On");
        return $"Go to Portal Row [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var selectAll = step.Element("SelectAll")?.Attribute("state")?.Value == "True";
        var location = step.Element("RowPageLocation")?.Attribute("value")?.Value ?? "Next";
        var exit = step.Element("Exit")?.Attribute("state")?.Value == "True";
        var calcEl = step.Element("Calculation");
        var calc = calcEl is not null ? Calculation.FromXml(calcEl) : null;
        return new GoToPortalRowStep(withDialog, selectAll, location, exit, calc, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
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
        return new GoToPortalRowStep(true, selectAll, location, exit, calc, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-portal-row.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "SelectAll", XmlElement = "SelectAll", XmlAttr = "state", Type = "boolean", HrLabel = "Select" },
            new ParamMetadata { Name = "RowPageLocation", XmlElement = "RowPageLocation", XmlAttr = "value", Type = "enum", ValidValues = ["First", "Last", "Previous", "Next", "ByCalculation"] },
            new ParamMetadata { Name = "Exit", XmlElement = "Exit", XmlAttr = "state", Type = "boolean", HrLabel = "Exit after last" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
