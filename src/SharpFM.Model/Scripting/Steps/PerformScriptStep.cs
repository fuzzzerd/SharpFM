using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Perform Script" step.
/// Carries a <see cref="PerformScriptTarget"/> discriminated union
/// (ByReference / ByCalculation) and an optional parameter Calculation.
/// Display uses the <c>(#id)</c> lossless-id convention for static refs.
/// </summary>
public sealed class PerformScriptStep : ScriptStep, IStepFactory
{
    public const int XmlId = 1;
    public const string XmlName = "Perform Script";

    public PerformScriptTarget Target { get; set; }
    public Calculation? Parameter { get; set; }

    public PerformScriptStep(bool enabled, PerformScriptTarget target, Calculation? parameter = null)
        : base(null, enabled)
    {
        Target = target;
        Parameter = parameter;
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";

        var calculatedEl = step.Element("Calculated");
        var scriptEl = step.Element("Script");
        var paramCalcEl = step.Element("Calculation");

        PerformScriptTarget target;
        if (calculatedEl is not null)
        {
            var calcEl = calculatedEl.Element("Calculation");
            var nameCalc = calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");
            target = new PerformScriptTarget.ByCalculation(nameCalc);
        }
        else if (scriptEl is not null)
        {
            target = new PerformScriptTarget.ByReference(NamedRef.FromXml(scriptEl));
        }
        else
        {
            // Degenerate: no script reference at all. Default to an empty by-ref.
            target = new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        }

        var parameter = paramCalcEl is not null ? Calculation.FromXml(paramCalcEl) : null;

        return new PerformScriptStep(enabled, target, parameter);
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

        // FM Pro emits parameter before Script for ByReference,
        // but after Calculated for ByCalculation. Match per-variant
        // order for clipboard-round-trip fidelity.
        switch (Target)
        {
            case PerformScriptTarget.ByReference byRef:
                if (Parameter is not null)
                    step.Add(Parameter.ToXml());
                step.Add(byRef.Script.ToXml("Script"));
                break;

            case PerformScriptTarget.ByCalculation byCalc:
                step.Add(new XElement("Calculated", byCalc.NameCalc.ToXml()));
                if (Parameter is not null)
                    step.Add(Parameter.ToXml());
                break;
        }

        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new List<string>();

        switch (Target)
        {
            case PerformScriptTarget.ByReference byRef:
                // Suppress (#0) when we don't actually have a script id to
                // preserve — same convention as GoToLayoutStep.
                parts.Add(byRef.Script.Id == 0
                    ? $"\"{byRef.Script.Name}\""
                    : $"\"{byRef.Script.Name}\" (#{byRef.Script.Id})");
                break;

            case PerformScriptTarget.ByCalculation byCalc:
                parts.Add($"By name: {byCalc.NameCalc.Text}");
                break;
        }

        if (Parameter is not null)
            parts.Add($"Parameter: {Parameter.Text}");

        return $"Perform Script [ {string.Join(" ; ", parts)} ]";
    }

    private static readonly Regex NamedScriptToken = new(
        "^\"(?<name>.*)\"\\s*\\(#(?<id>\\d+)\\)$",
        RegexOptions.Compiled);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        PerformScriptTarget target = new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        Calculation? parameter = null;

        foreach (var raw in hrParams)
        {
            var token = raw.Trim();

            if (token.StartsWith("Parameter:", StringComparison.OrdinalIgnoreCase))
            {
                var expr = token.Substring("Parameter:".Length).Trim();
                if (!string.IsNullOrEmpty(expr))
                    parameter = new Calculation(expr);
            }
            else if (token.StartsWith("By name:", StringComparison.OrdinalIgnoreCase))
            {
                var expr = token.Substring("By name:".Length).Trim();
                target = new PerformScriptTarget.ByCalculation(new Calculation(expr));
            }
            else
            {
                var match = NamedScriptToken.Match(token);
                if (match.Success)
                {
                    var name = match.Groups["name"].Value;
                    var id = int.Parse(match.Groups["id"].Value);
                    target = new PerformScriptTarget.ByReference(new NamedRef(id, name));
                }
                else if (token.StartsWith("\"") && token.EndsWith("\"") && token.Length >= 2)
                {
                    var name = token.Substring(1, token.Length - 2);
                    target = new PerformScriptTarget.ByReference(new NamedRef(0, name));
                }
            }
        }

        return new PerformScriptStep(enabled, target, parameter);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-script.html",
        Params =
        [
            new ParamMetadata { Name = "Script", XmlElement = "Script", Type = "script" },
            new ParamMetadata { Name = "Parameter", XmlElement = "Calculation", Type = "calculation", HrLabel = "Parameter" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
