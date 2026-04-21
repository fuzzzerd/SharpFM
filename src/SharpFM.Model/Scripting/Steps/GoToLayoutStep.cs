using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
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
public sealed class GoToLayoutStep : ScriptStep, IStepFactory
{
    public const int XmlId = 6;
    public const string XmlName = "Go to Layout";

    public LayoutTarget Target { get; set; }
    public Animation? Animation { get; set; }

    public GoToLayoutStep(bool enabled, LayoutTarget target, Animation? animation = null)
        : base(null, enabled)
    {
        Target = target;
        Animation = animation;
    }

    // --- XML parse ---

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
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

        return new GoToLayoutStep(enabled, target, animation);
    }

    private static Calculation ReadCalculationFromLayout(XElement? layoutEl)
    {
        var calcEl = layoutEl?.Element("Calculation");
        return calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");
    }

    // --- XML emit ---

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

        step.Add(new XElement("LayoutDestination",
            new XAttribute("value", Target.WireValue)));

        switch (Target)
        {
            case LayoutTarget.Original:
                // No <Layout> element at all.
                break;

            case LayoutTarget.Named named:
                // NamedRef.ToXml emits id+name attributes — the real id is
                // carried from FromXml through the POCO, not silently
                // replaced with zero.
                step.Add(named.Layout.ToXml("Layout"));
                break;

            case LayoutTarget.ByNameCalc byName:
                step.Add(new XElement("Layout", byName.Calc.ToXml()));
                break;

            case LayoutTarget.ByNumberCalc byNumber:
                step.Add(new XElement("Layout", byNumber.Calc.ToXml()));
                break;
        }

        if (Animation is not null)
            step.Add(Animation.ToXml());

        return step;
    }

    // --- Display text render ---

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

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
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

        return new GoToLayoutStep(enabled, target, animation);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-layout.html",
        Params =
        [
            new ParamMetadata { Name = "LayoutDestination", XmlElement = "LayoutDestination", XmlAttr = "value", Type = "enum", ValidValues = ["OriginalLayout", "SelectedLayout", "LayoutNameByCalc", "LayoutNumberByCalc"], DefaultValue = "SelectedLayout" },
            new ParamMetadata { Name = "Layout", XmlElement = "Layout", Type = "layout" },
            new ParamMetadata { Name = "Animation", XmlElement = "Animation", Type = "enum" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
