using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Perform Script" step.
/// Carries a <see cref="PerformScriptTarget"/> discriminated union
/// (ByReference / ByCalculation) and an optional parameter Calculation.
/// Display uses the <c>(#id)</c> lossless-id convention for static refs.
/// </summary>
public sealed class PerformScriptStep : ScriptStep<PerformScriptStep>, IStepFactory
{
    public const int XmlId = 1;
    public const string XmlName = "Perform Script";

    public PerformScriptTarget Target { get; set; }
    public Calculation? Parameter { get; set; }

    // Emit-only wire projections: FM Pro emits the parameter Calculation
    // before <Script> for ByReference but after <Calculated> for
    // ByCalculation. Get-only, so the shape parser skips them.
    public Calculation? ParameterBeforeScript =>
        Target is PerformScriptTarget.ByReference ? Parameter : null;
    public Calculation? ParameterAfterCalculated =>
        Target is PerformScriptTarget.ByCalculation ? Parameter : null;

    private PerformScriptStep() : base(false)
    {
        Target = new PerformScriptTarget.ByReference(new NamedRef(0, ""));
    }

    public PerformScriptStep(bool enabled, PerformScriptTarget target, Calculation? parameter = null)
        : base(enabled)
    {
        Target = target;
        Parameter = parameter;
    }

    // Hand-written: tolerates the degenerate no-reference form and binds
    // the step-level Parameter regardless of which side of the variant it
    // appeared on.
    protected internal override void PopulateFromXml(XElement step)
    {
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

        Target = target;
        Parameter = parameter;
    }

    // Hand-written: the script target is a variant (by-name renders the quoted name, by-calculation the expression) the shape renderer cannot express.
    public override string ToDisplayLine()
    {
        var parts = new List<string>();

        switch (Target)
        {
            case PerformScriptTarget.ByReference byRef:
                // Suppress (#0) when we don't actually have a script id to
                // preserve — same convention as GoToLayoutStep.
                parts.Add(DisplayQuoting.QuoteWithId(byRef.Script.Name, byRef.Script.Id));
                break;

            case PerformScriptTarget.ByCalculation byCalc:
                parts.Add($"By name: {byCalc.NameCalc.Text}");
                break;
        }

        if (Parameter is not null)
            parts.Add($"Parameter: {Parameter.Text}");

        return $"Perform Script [ {string.Join(" ; ", parts)} ]";
    }

    protected internal override void PopulateFromDisplay(string[] hrParams)
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
            else if (DisplayQuoting.TryParseNamedRef(token, out var namedRef))
            {
                target = new PerformScriptTarget.ByReference(namedRef);
            }
        }

        Target = target;
        Parameter = parameter;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-script.html",
        Shape =
        [
            new BareCalcChild { PocoProperty = "ParameterBeforeScript", Optional = true, HrLabel = "Parameter", Display = DisplayMode.Augmented },
            new VariantBlock(
            [
                new VariantCase(typeof(PerformScriptTarget.ByReference),
                    [new NamedRefChild("Script")]) { MatchElement = "Script" },
                new VariantCase(typeof(PerformScriptTarget.ByCalculation),
                    [new NamedCalcChild("Calculated") { PocoProperty = "NameCalc" }])
                { MatchElement = "Calculated" },
            ]) { PocoProperty = "Target", Required = true },
            new BareCalcChild { PocoProperty = "ParameterAfterCalculated", Optional = true, Display = DisplayMode.Hidden },
        ],
    };
}
