using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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

    public string Name { get; set; } = "";
    public Calculation Value { get; set; } = new("");
    public Calculation Repetition { get; set; } = new("1");

    private SetVariableStep() : base(false) { }

    public SetVariableStep(bool enabled, string name, Calculation value, Calculation? repetition = null)
        : base(enabled)
    {
        Name = name;
        Value = value;
        Repetition = repetition ?? new Calculation("1");
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetVariableStep>(step, Metadata);

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: the name and repetition merge into one [rep] token the shape renderer cannot express.
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
        // Canonical shape per skill §3 / §7.1 / §7.4: Value, Repetition, then
        // Name LAST as a plain-text child (the variable-name silent-drop trap).
        // Repetition is always emitted in XML (never Optional) but hidden from
        // the display line when it holds the default "1".
        Shape =
        [
            new NamedCalcChild("Value") { PocoProperty = "Value", HrLabel = "Value", Required = true, Display = DisplayMode.Augmented },
            new NamedCalcChild("Repetition") { PocoProperty = "Repetition", Display = DisplayMode.Augmented, DefaultValue = "1" },
            new NamedTextChild("Name") { PocoProperty = "Name", Required = true, Display = DisplayMode.Native },
        ],
        // Params is retained alongside Shape only to feed the not-yet-migrated
        // legacy consumers (FmScript.SynthesizeHrParams, ScriptValidator). It is
        // deleted at the end of phase 6 once those read Shape directly.
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
