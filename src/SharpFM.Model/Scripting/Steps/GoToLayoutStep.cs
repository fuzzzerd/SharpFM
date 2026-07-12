using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Go to Layout. Carries a <see cref="LayoutTarget"/> discriminated union
/// that matches FM's four LayoutDestination modes (Original, Selected /
/// named, ByNameCalc, ByNumberCalc) plus an optional <see cref="Animation"/>.
/// Serialization is a pure function of these typed fields; round-trips
/// preserve the layout id, any nested Calculation, and the animation
/// wire value byte-for-byte.
///
/// <para>
/// Display text uses a <c>(#id)</c> suffix for named layouts so the
/// layout id survives display-text edits — FM Pro itself renders only
/// the layout's base table in that slot, which we can't derive without
/// schema access.
/// </para>
/// </summary>
public sealed class GoToLayoutStep : ScriptStep<GoToLayoutStep>, IStepFactory
{
    public const int XmlId = 6;
    public const string XmlName = "Go to Layout";

    public LayoutTarget Target { get; set; }
    public Animation? Animation { get; set; }

    private GoToLayoutStep() : base(false)
    {
        Target = new LayoutTarget.Original();
    }

    public GoToLayoutStep(bool enabled, LayoutTarget target, Animation? animation = null)
        : base(enabled)
    {
        Target = target;
        Animation = animation;
    }

    // --- XML parse ---

    // Hand-written: the reader is deliberately more tolerant than the
    // canonical shape — it accepts legacy wire-value aliases and degrades a
    // SelectedLayout with no named reference to Original instead of
    // materializing an empty target.
    protected internal override void PopulateFromXml(XElement step)
    {
        var destWire = step.Element("LayoutDestination")?.Attribute("value")?.Value ?? "SelectedLayout";
        var layoutEl = step.Element("Layout");

        LayoutTarget target = destWire switch
        {
            "OriginalLayout" => new LayoutTarget.Original(),

            // Accept legacy wire values (pre-catalog-fix data) alongside
            // the real FM Pro values. New data always uses the short forms.
            "LayoutNameByCalc" or "LayoutNameByCalculation"
                => new LayoutTarget.ByNameCalc(ReadCalculationFromLayout(layoutEl)),
            "LayoutNumberByCalc" or "LayoutNumberByCalculation"
                => new LayoutTarget.ByNumberCalc(ReadCalculationFromLayout(layoutEl)),

            // SelectedLayout (the default) with a present <Layout> element
            // carries the named reference. Missing Layout degrades to
            // Original — an edge case that only arises in hand-crafted XML.
            _ => layoutEl is not null && layoutEl.Attribute("name") is not null
                ? new LayoutTarget.Named(NamedRef.FromXml(layoutEl))
                : new LayoutTarget.Original()
        };

        var animationEl = step.Element("Animation");
        var animation = animationEl is not null ? Values.Animation.FromXml(animationEl) : null;

        Target = target;
        Animation = animation;
    }

    private static Calculation ReadCalculationFromLayout(XElement? layoutEl)
    {
        var calcEl = layoutEl?.Element("Calculation");
        return calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");
    }

    // --- Display text render ---

    // Hand-written: quoted "layout name" / original-layout variant grammar
    // the shape renderer cannot produce.
    public override string ToDisplayLine()
    {
        var parts = new List<string>();

        switch (Target)
        {
            case LayoutTarget.Original:
                parts.Add("original layout");
                break;

            case LayoutTarget.Named named:
                // id=0 is the "unknown" sentinel (user edited display text
                // and dropped the suffix, or caller constructed without an
                // id). Suppressing (#0) keeps the display clean when we
                // don't actually have an id to preserve.
                parts.Add(named.Layout.Id == 0
                    ? $"\"{named.Layout.Name}\""
                    : $"\"{named.Layout.Name}\" (#{named.Layout.Id})");
                break;

            case LayoutTarget.ByNameCalc byName:
                parts.Add($"Layout Name: {byName.Calc.Text}");
                break;

            case LayoutTarget.ByNumberCalc byNumber:
                parts.Add($"Layout Number: {byNumber.Calc.Text}");
                break;
        }

        if (Animation is not null)
            parts.Add($"Animation: {Animation.WireValue}");

        return $"Go to Layout [ {string.Join(" ; ", parts)} ]";
    }

    // --- Display text parse ---

    private static readonly Regex NamedLayoutToken = new(
        "^\"(?<name>.*)\"\\s*\\(#(?<id>\\d+)\\)$",
        RegexOptions.Compiled);

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        LayoutTarget target = new LayoutTarget.Original();
        Animation? animation = null;

        foreach (var raw in hrParams)
        {
            var token = raw.Trim();

            if (token.StartsWith("Animation:", StringComparison.OrdinalIgnoreCase))
            {
                var wire = token.Substring("Animation:".Length).Trim();
                if (!string.IsNullOrEmpty(wire))
                    animation = new Animation(wire);
            }
            else if (token.Equals("original layout", StringComparison.OrdinalIgnoreCase))
            {
                target = new LayoutTarget.Original();
            }
            else if (token.StartsWith("Layout Name:", StringComparison.OrdinalIgnoreCase))
            {
                var expr = token.Substring("Layout Name:".Length).Trim();
                target = new LayoutTarget.ByNameCalc(new Calculation(expr));
            }
            else if (token.StartsWith("Layout Number:", StringComparison.OrdinalIgnoreCase))
            {
                var expr = token.Substring("Layout Number:".Length).Trim();
                target = new LayoutTarget.ByNumberCalc(new Calculation(expr));
            }
            else
            {
                // Named layout with (#id) suffix is the lossless form.
                // Bare quoted names without an id degrade to a NamedRef
                // with id 0 — the user edited the display text and
                // dropped the id, there's nothing better we can do.
                var match = NamedLayoutToken.Match(token);
                if (match.Success)
                {
                    var name = match.Groups["name"].Value;
                    var id = int.Parse(match.Groups["id"].Value);
                    target = new LayoutTarget.Named(new NamedRef(id, name));
                }
                else if (token.StartsWith("\"") && token.EndsWith("\"") && token.Length >= 2)
                {
                    var name = token.Substring(1, token.Length - 2);
                    target = new LayoutTarget.Named(new NamedRef(0, name));
                }
            }
        }

        Target = target;
        Animation = animation;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-layout.html",
        Shape =
        [
            new VariantBlock(
            [
                new VariantCase(typeof(LayoutTarget.Original),
                    [new EnumValueChild("LayoutDestination") { PocoProperty = "WireValue" }])
                { MatchElement = "LayoutDestination", MatchValues = ["OriginalLayout"] },
                new VariantCase(typeof(LayoutTarget.ByNameCalc),
                    [
                        new EnumValueChild("LayoutDestination") { PocoProperty = "WireValue" },
                        new NamedCalcChild("Layout") { PocoProperty = "Calc" },
                    ])
                { MatchElement = "LayoutDestination", MatchValues = ["LayoutNameByCalc", "LayoutNameByCalculation"] },
                new VariantCase(typeof(LayoutTarget.ByNumberCalc),
                    [
                        new EnumValueChild("LayoutDestination") { PocoProperty = "WireValue" },
                        new NamedCalcChild("Layout") { PocoProperty = "Calc" },
                    ])
                { MatchElement = "LayoutDestination", MatchValues = ["LayoutNumberByCalc", "LayoutNumberByCalculation"] },
                new VariantCase(typeof(LayoutTarget.Named),
                    [
                        new EnumValueChild("LayoutDestination") { PocoProperty = "WireValue" },
                        new NamedRefChild("Layout"),
                    ])
                { MatchElement = "LayoutDestination" },
            ]) { PocoProperty = "Target", Required = true, Display = DisplayMode.Hidden },
            new HrOnly("LayoutDestination") { DisplayValues = ["OriginalLayout", "SelectedLayout", "LayoutNameByCalc", "LayoutNumberByCalc"] },
            new HrOnly("Layout"),
            new ValueTypeChild("Animation") { Display = DisplayMode.Augmented },
        ],
    };
}
