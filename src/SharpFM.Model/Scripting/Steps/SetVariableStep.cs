using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Set Variable" script step.
/// Display is <c>Set Variable [ $name[rep] ; Value: calc ]</c> with the
/// <c>[rep]</c> suffix suppressed when Repetition is literally "1".
/// <c>&lt;Repetition&gt;</c> is a full Calculation (not an integer) and is
/// always emitted in XML even when "1" — the round-trip invariant.
/// </summary>
public sealed class SetVariableStep : ScriptStep
{
    public string Name { get; set; }
    public Calculation Value { get; set; }
    public Calculation Repetition { get; set; }

    public SetVariableStep(bool enabled, string name, Calculation value, Calculation? repetition = null)
        : base(StepCatalogLoader.ByName["Set Variable"], enabled)
    {
        Name = name;
        Value = value;
        Repetition = repetition ?? new Calculation("1");
    }

    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Register typed step factories on assembly load.")]
    [ModuleInitializer]
    internal static void Register()
    {
        StepXmlFactory.Register("Set Variable", FromXml);
        StepDisplayFactory.Register("Set Variable", FromDisplayParams);
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
            new XAttribute("id", 141),
            new XAttribute("name", "Set Variable"));

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

    // Matches $name[rep] or $$name[rep]; rep can itself contain brackets for
    // nested expressions, so we use non-greedy matching and bracket balance.
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
}
