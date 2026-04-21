using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExecuteFileMakerDataApiStep : ScriptStep, IStepFactory
{
    public const int XmlId = 203;
    public const string XmlName = "Execute FileMaker Data API";

    public bool Select { get; set; }
    public FieldRef? Target { get; set; }
    public Calculation Query { get; set; }

    public ExecuteFileMakerDataApiStep(
        bool select = true,
        FieldRef? target = null,
        Calculation? query = null,
        bool enabled = true)
        : base(enabled)
    {
        Select = select;
        Target = target;
        Query = query ?? new Calculation("");
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("SelectAll", new XAttribute("state", Select ? "True" : "False")),
            Query.ToXml("Calculation"));
        if (Target is not null)
        {
            if (Target.IsVariable) step.Add(new XElement("Text"));
            step.Add(Target.ToXml("Field"));
        }
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        parts.Add($"Select: {(Select ? "On" : "Off")}");
        if (Target is not null) parts.Add($"Target: {Target.ToDisplayString()}");
        parts.Add(Query.Text);
        return $"Execute FileMaker Data API [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var select = step.Element("SelectAll")?.Attribute("state")?.Value == "True";
        var calcEl = step.Element("Calculation");
        var query = calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new ExecuteFileMakerDataApiStep(select, target, query, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool select = true;
        FieldRef? target = null;
        Calculation query = new("");
        bool querySeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Select:", StringComparison.OrdinalIgnoreCase))
                select = t.Substring(7).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            else if (!querySeen && !string.IsNullOrWhiteSpace(t))
            {
                query = new Calculation(t);
                querySeen = true;
            }
        }
        return new ExecuteFileMakerDataApiStep(select, target, query, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/execute-filemaker-data-api.html",
        Params =
        [
            new ParamMetadata { Name = "SelectAll", XmlElement = "SelectAll", XmlAttr = "state", Type = "boolean", HrLabel = "Select" },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Target" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation", Required = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
