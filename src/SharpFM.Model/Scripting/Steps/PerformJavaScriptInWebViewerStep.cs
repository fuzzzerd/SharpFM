using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Perform JavaScript in Web Viewer. Calls a JavaScript function in a
/// layout's web viewer with an optional ObjectName target and a parameter
/// list. Parameters are wrapped in <c>&lt;P&gt;&lt;Calculation/&gt;&lt;/P&gt;</c>
/// children under a <c>&lt;Parameters Count="N"&gt;</c> element.
/// </summary>
public sealed class PerformJavaScriptInWebViewerStep : ScriptStep, IStepFactory
{
    public const int XmlId = 175;
    public const string XmlName = "Perform JavaScript in Web Viewer";

    public Calculation? ObjectName { get; set; }
    public Calculation FunctionName { get; set; }
    public IReadOnlyList<Calculation> Parameters { get; set; }

    public PerformJavaScriptInWebViewerStep(
        Calculation? objectName = null,
        Calculation? functionName = null,
        IReadOnlyList<Calculation>? parameters = null,
        bool enabled = true)
        : base(enabled)
    {
        ObjectName = objectName;
        FunctionName = functionName ?? new Calculation("\"\"");
        Parameters = parameters ?? new List<Calculation>();
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));
        if (ObjectName is not null) step.Add(new XElement("ObjectName", ObjectName.ToXml("Calculation")));
        step.Add(new XElement("FunctionName", FunctionName.ToXml("Calculation")));
        if (Parameters.Count > 0)
        {
            var parameters = new XElement("Parameters", new XAttribute("Count", Parameters.Count));
            foreach (var p in Parameters)
                parameters.Add(new XElement("P", p.ToXml("Calculation")));
            step.Add(parameters);
        }
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new List<string>();
        if (ObjectName is not null) parts.Add($"Object Name: {ObjectName.Text}");
        parts.Add($"Function Name: {FunctionName.Text}");
        if (Parameters.Count > 0)
            parts.Add($"Parameters: {string.Join(", ", Parameters.Select(p => p.Text))}");
        return $"Perform JavaScript in Web Viewer [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var objEl = step.Element("ObjectName")?.Element("Calculation");
        var obj = objEl is not null ? Calculation.FromXml(objEl) : null;
        var fnEl = step.Element("FunctionName")?.Element("Calculation");
        var fn = fnEl is not null ? Calculation.FromXml(fnEl) : new Calculation("\"\"");
        var paramsList = step.Element("Parameters")?.Elements("P")
            .Select(p => p.Element("Calculation") is { } c ? Calculation.FromXml(c) : new Calculation(""))
            .ToList() ?? new List<Calculation>();
        return new PerformJavaScriptInWebViewerStep(obj, fn, paramsList, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        Calculation? obj = null;
        Calculation fn = new("\"\"");
        List<Calculation> parameters = new();
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Object Name:", System.StringComparison.OrdinalIgnoreCase))
                obj = new Calculation(t.Substring(12).Trim());
            else if (t.StartsWith("Function Name:", System.StringComparison.OrdinalIgnoreCase))
                fn = new Calculation(t.Substring(14).Trim());
            else if (t.StartsWith("Parameters:", System.StringComparison.OrdinalIgnoreCase))
            {
                var list = t.Substring(11).Trim();
                foreach (var p in list.Split(','))
                    parameters.Add(new Calculation(p.Trim()));
            }
        }
        return new PerformJavaScriptInWebViewerStep(obj, fn, parameters, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-javascript-in-web-viewer.html",
        Params =
        [
            new ParamMetadata { Name = "ObjectName", XmlElement = "ObjectName", Type = "namedCalc", HrLabel = "Object Name" },
            new ParamMetadata { Name = "FunctionName", XmlElement = "FunctionName", Type = "namedCalc", HrLabel = "Function Name", Required = true },
            new ParamMetadata { Name = "Parameters", XmlElement = "Parameters", Type = "complex", HrLabel = "Parameters" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
