using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpFM.Model.Scripting.Shapes;

/// <summary>
/// Human-readable projection of a step's <c>Shape</c> for the display-text
/// consumers (param synthesis, script validation, editor completion). The
/// shape lists nodes in XML order; display lines put FileMaker-native tokens
/// first and SharpFM's augmented <c>[Label: value]</c> tokens after, so this
/// view re-orders accordingly and drops wire-only plumbing.
/// </summary>
public static class ShapeHrView
{
    /// <summary>
    /// Display-ordered, flattened parameter slots: <see cref="DisplayMode.Native"/>
    /// nodes first (in shape order), then <see cref="DisplayMode.Augmented"/>;
    /// <see cref="DisplayMode.Hidden"/> nodes are excluded. Wrapper children
    /// flatten into the list (they bind the owning step's properties); a
    /// <see cref="VariantBlock"/> contributes itself as a single slot;
    /// <see cref="Passthrough"/> and <see cref="AttributeNode"/> are wire-only
    /// and never surface.
    /// </summary>
    public static IReadOnlyList<ShapeNode> HrNodes(IReadOnlyList<ShapeNode> shape)
    {
        var flat = new List<ShapeNode>();
        Flatten(shape, flat);
        return flat.Where(n => n.Display == DisplayMode.Native)
            .Concat(flat.Where(n => n.Display == DisplayMode.Augmented))
            .ToList();
    }

    private static void Flatten(IReadOnlyList<ShapeNode> shape, List<ShapeNode> into)
    {
        foreach (var node in shape)
        {
            switch (node)
            {
                case Passthrough or AttributeNode:
                    continue;
                case WrapperChild w:
                    Flatten(w.Children, into);
                    continue;
                default:
                    if (node.Display != DisplayMode.Hidden) into.Add(node);
                    continue;
            }
        }
    }

    /// <summary>Canonical parameter name of a slot: the bound property, falling back to its primary element.</summary>
    public static string NameOf(ShapeNode node) => node switch
    {
        HrOnly h => h.Name,
        _ => node.PocoProperty ?? PrimaryElementOf(node) ?? "",
    };

    /// <summary>
    /// True when <paramref name="key"/> addresses this slot. Callers may use
    /// the bound property name, the XML element name, or the display label —
    /// all case-insensitive — so pre-cutover param names keep working.
    /// </summary>
    public static bool MatchesName(ShapeNode node, string key) =>
        string.Equals(NameOf(node), key, StringComparison.OrdinalIgnoreCase)
        || string.Equals(node.HrLabel, key, StringComparison.OrdinalIgnoreCase)
        || Serialization.StepXmlValidator.ElementNamesOf(node)
            .Any(e => string.Equals(e, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>True for presence/state booleans, whose display values are On/Off.</summary>
    public static bool IsBooleanLike(ShapeNode node) =>
        node is BoolStateChild or FlagChild or HrOnly { Boolean: true };

    /// <summary>
    /// Valid display-text values for completion and validation: the node's
    /// <see cref="ShapeNode.DisplayValues"/> when the display forms differ from
    /// the wire forms, On/Off for boolean-like nodes, then the wire
    /// <see cref="ShapeNode.ValidValues"/>.
    /// </summary>
    public static IReadOnlyList<string> DisplayValuesOf(ShapeNode node)
    {
        if (node.DisplayValues is { Count: > 0 } dv) return dv;
        if (IsBooleanLike(node)) return new[] { "On", "Off" };
        return node.ValidValues ?? Array.Empty<string>();
    }

    /// <summary>Short kind label shown as the completion description.</summary>
    public static string KindOf(ShapeNode node) => node switch
    {
        HrOnly h => h.Boolean ? "boolean" : "option",
        FlagChild => "flag",
        BoolStateChild => "boolean",
        EnumValueChild => "enum",
        BareCalcChild or NamedCalcChild => "calculation",
        NamedTextChild => "text",
        FieldChild => "field",
        NamedRefChild => "reference",
        ValueTypeChild => "options",
        ParametersList => "parameters",
        VariantBlock => "target",
        _ => "value",
    };

    private static string? PrimaryElementOf(ShapeNode node) =>
        Serialization.StepXmlValidator.ElementNamesOf(node).FirstOrDefault();
}
