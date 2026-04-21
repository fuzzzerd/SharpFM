using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Set Variable" script step.
/// Display is <c>Set Variable [ $name[rep] ; Value: calc ]</c> with the
/// <c>[rep]</c> suffix suppressed when Repetition is literally "1".
/// <c>&lt;Repetition&gt;</c> is a full Calculation (not an integer) and is
/// always emitted in XML even when "1" — the round-trip invariant.
/// </summary>
public sealed class SetVariableStep : ScriptStep, IStepFactory
{
    public const int XmlId = 141;
    public const string XmlName = "Set Variable";

    public string Name { get; set; }
    public Calculation Value { get; set; }
    public Calculation Repetition { get; set; }

    public SetVariableStep(bool enabled, string name, Calculation value, Calculation? repetition = null)
        : base(enabled)
    {
        Name = name;
        Value = value;
        Repetition = repetition ?? new Calculation("1");
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";

        var name = step.Element("Name")?.Value ?? "";

        var valueCalc = step.Element("Value")?.Element("Calculation");
        var value = valueCalc is not null ? Calculation.FromXml(valueCalc) : new Calculation("");

        var repCalc = step.Element("Repetition")?.Element("Calculation");
        var repetition = repCalc is not null ? Calculation.FromXml(repCalc) : new Calculation("1");

        return new SetVariableStep(enabled, name, value, repetition);
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

        step.Add(new XElement("Value", Value.ToXml()));
        step.Add(new XElement("Repetition", Repetition.ToXml()));
        step.Add(new XElement("Name", Name));

        return step;
    }

    public override string ToDisplayLine()
    {
        var nameToken = Repetition.Text == "1"
            ? Name
            : $"{Name}[{Repetition.Text}]";

        return $"Set Variable [ {nameToken} ; Value: {Value.Text} ]";
    }

    private static readonly Regex VarNameWithRep = new(
        @"^(?<name>\$+[^\[]+)\[(?<rep>.*)\]$",
        RegexOptions.Compiled);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var name = "";
        var value = new Calculation("");
        Calculation repetition = new("1");

        if (hrParams.Length >= 1)
        {
            var nameToken = hrParams[0].Trim();
            var match = VarNameWithRep.Match(nameToken);
            if (match.Success)
            {
                name = match.Groups["name"].Value.Trim();
                repetition = new Calculation(match.Groups["rep"].Value);
            }
            else
            {
                name = nameToken;
            }
        }

        if (hrParams.Length >= 2)
        {
            var valueToken = hrParams[1].Trim();
            if (valueToken.StartsWith("Value:", StringComparison.OrdinalIgnoreCase))
                valueToken = valueToken.Substring("Value:".Length).Trim();
            value = new Calculation(valueToken);
        }

        return new SetVariableStep(enabled, name, value, repetition);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-variable.html",
        Params =
        [
            new ParamMetadata { Name = "Name", XmlElement = "Name", Type = "text", Required = true },
            new ParamMetadata { Name = "Value", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Value", Required = true },
            new ParamMetadata { Name = "Repetition", XmlElement = "Calculation", Type = "namedCalc" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
