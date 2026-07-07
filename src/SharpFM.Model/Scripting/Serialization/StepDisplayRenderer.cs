using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Renders a step's human-readable display line from the same
/// <see cref="StepMetadata.Shape"/> that drives XML emission, reproducing the
/// shipped display grammar exactly:
///
/// <para>
/// <c>Step Name [ native ; Label: value ; Label: value ]</c>
/// </para>
///
/// <list type="bullet">
///   <item>Slots render in <see cref="Shapes.ShapeHrView.HrNodes"/> order
///   (<see cref="DisplayMode.Native"/> first, then
///   <see cref="DisplayMode.Augmented"/>) inside one bracket, joined by
///   <c>;</c> — the way FileMaker Pro shows them.</item>
///   <item>A labeled slot renders as <c>HrLabel: value</c>; an unlabeled
///   native renders bare.</item>
///   <item><see cref="DisplayMode.Hidden"/> nodes never appear.</item>
/// </list>
///
/// Nodes whose value is blank are dropped; an augmented node whose value equals
/// its <see cref="ShapeNode.DefaultValue"/> is also dropped (so a child that is
/// always emitted in XML can still be hidden from the display line when it holds
/// its default — e.g. Set Variable's Repetition of "1").
/// </summary>
public static class StepDisplayRenderer
{
    public static string Render(ScriptStep step, StepMetadata meta)
    {
        var tokens = new List<string>();

        foreach (var node in ShapeHrView.HrNodes(meta.Shape))
        {
            var value = DisplayValue(step, node);
            if (string.IsNullOrEmpty(value)) continue;

            if (node.Display == DisplayMode.Augmented)
            {
                // Display suppression is keyed on DefaultValue alone, decoupled
                // from the XML-emission Optional flag: a child can be always
                // emitted in XML (e.g. Set Variable's Repetition) yet hidden in
                // the display line when it holds its default.
                if (node.DefaultValue is not null && value == node.DefaultValue) continue;
                tokens.Add($"{node.HrLabel ?? ElementLabel(node)}: {value}");
            }
            else
            {
                tokens.Add(node.HrLabel is not null ? $"{node.HrLabel}: {value}" : value);
            }
        }

        return tokens.Count == 0
            ? meta.Name
            : $"{meta.Name} [ {string.Join(" ; ", tokens)} ]";
    }

    private static string? DisplayValue(object src, ShapeNode node) => node switch
    {
        AttributeNode a => Get(src, a.PocoProperty ?? a.AttrName)?.ToString(),
        BoolStateChild b => (bool)(Get(src, b.PocoProperty ?? b.Element) ?? false) ? "On" : "Off",
        EnumValueChild e => ToDisplayValue(node, Get(src, e.PocoProperty ?? e.Element)?.ToString()),
        BareCalcChild => (Get(src, node.PocoProperty ?? "Calculation") as Calculation)?.Text,
        NamedCalcChild nc => (Get(src, nc.PocoProperty ?? nc.Element) as Calculation)?.Text,
        NamedTextChild nt => Get(src, nt.PocoProperty ?? nt.Element)?.ToString(),
        FieldChild f => (Get(src, f.PocoProperty ?? f.Element) as FieldRef)?.ToDisplayString(),
        NamedRefChild nr => (Get(src, nr.PocoProperty ?? nr.Element) as NamedRef)?.Name,
        ParametersList pl => FormatList(Get(src, pl.PocoProperty ?? pl.Wrapper)),
        // ValueTypeChild / VariantBlock / Passthrough / HrOnly have no uniform
        // display contribution; steps needing them keep hand-written display.
        _ => null,
    };

    /// <summary>
    /// Translate a wire value to its display form via the node's parallel
    /// ValidValues/DisplayValues lists (e.g. "SortAscending" → "Ascending").
    /// Values outside the declared set pass through unchanged.
    /// </summary>
    private static string? ToDisplayValue(ShapeNode node, string? wire)
    {
        if (wire is null || node.DisplayValues is null || node.ValidValues is null) return wire;
        var i = node.ValidValues.ToList().IndexOf(wire);
        return i >= 0 && i < node.DisplayValues.Count ? node.DisplayValues[i] : wire;
    }

    private static string? FormatList(object? value) =>
        value is IEnumerable e and not string
            ? string.Join(", ", e.Cast<object>().Select(o => o is Calculation c ? c.Text : o?.ToString()))
            : null;

    private static string ElementLabel(ShapeNode node) => node switch
    {
        BoolStateChild b => b.Element,
        EnumValueChild e => e.Element,
        NamedCalcChild nc => nc.Element,
        NamedTextChild nt => nt.Element,
        FieldChild f => f.Element,
        NamedRefChild nr => nr.Element,
        ValueTypeChild vt => vt.Element,
        AttributeNode a => a.AttrName,
        _ => "value",
    };

    private static object? Get(object src, string name) => ShapeReflection.Get(src, name);
}
