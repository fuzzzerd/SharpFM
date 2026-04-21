using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Go to Record/Request/Page. Navigates to First/Last/Previous/Next/ByCalculation.
/// Calculation is only used when RowPageLocation = ByCalculation.
/// Exit flag only applies with Previous / Next.
/// </summary>
public sealed class GoToRecordRequestPageStep : ScriptStep, IStepFactory
{
    public const int XmlId = 16;
    public const string XmlName = "Go to Record/Request/Page";

    public bool WithDialog { get; set; }
    public string Location { get; set; }
    public bool ExitAfterLast { get; set; }
    public Calculation? Calculation { get; set; }

    public GoToRecordRequestPageStep(
        bool withDialog = true,
        string location = "Next",
        bool exitAfterLast = false,
        Calculation? calculation = null,
        bool enabled = true)
        : base(null, enabled)
    {
        WithDialog = withDialog;
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
            new XElement("RowPageLocation", new XAttribute("value", Location)));
        if (ExitAfterLast && (Location == "Previous" || Location == "Next"))
            step.Add(new XElement("Exit", new XAttribute("state", "True")));
        if (Location == "ByCalculation" && Calculation is not null)
            step.Add(Calculation.ToXml("Calculation"));
        return step;
    }

    public override string ToDisplayLine()
    {
        var loc = Location switch
        {
            "First" => "First",
            "Last" => "Last",
            "Previous" => "Previous",
            "Next" => "Next",
            "ByCalculation" => Calculation?.Text ?? "",
            _ => Location,
        };
        var parts = new System.Collections.Generic.List<string> { loc };
        if (ExitAfterLast) parts.Add("Exit after last: On");
        if (!WithDialog) parts.Add("With dialog: Off");
        return $"Go to Record/Request/Page [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var location = step.Element("RowPageLocation")?.Attribute("value")?.Value ?? "Next";
        var exit = step.Element("Exit")?.Attribute("state")?.Value == "True";
        var calcEl = step.Element("Calculation");
        var calc = calcEl is not null ? Calculation.FromXml(calcEl) : null;
        return new GoToRecordRequestPageStep(withDialog, location, exit, calc, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        string location = "Next";
        bool exit = false;
        bool withDialog = true;
        Calculation? calc = null;
        bool locSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Exit after last:", StringComparison.OrdinalIgnoreCase))
                exit = t.Substring(16).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (!locSeen && !string.IsNullOrWhiteSpace(t))
            {
                if (t == "First" || t == "Last" || t == "Previous" || t == "Next")
                    location = t;
                else
                {
                    location = "ByCalculation";
                    calc = new Calculation(t);
                }
                locSeen = true;
            }
        }
        return new GoToRecordRequestPageStep(withDialog, location, exit, calc, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-record-request-page.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "RowPageLocation", XmlElement = "RowPageLocation", XmlAttr = "value", Type = "enum", ValidValues = ["First", "Last", "Previous", "Next", "ByCalculation"], DefaultValue = "Next" },
            new ParamMetadata { Name = "Exit", XmlElement = "Exit", XmlAttr = "state", Type = "boolean", HrLabel = "Exit after last" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
