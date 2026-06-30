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
/// <see cref="StepMetadata.Shape"/> that drives XML emission, applying one
/// uniform convention so every step surfaces FileMaker-hidden values
/// identically:
///
/// <para>
/// <c>Step Name [ native ; native ] [Label: value] [Label: value]</c>
/// </para>
///
/// <list type="bullet">
///   <item><see cref="DisplayMode.Native"/> nodes render bare inside the main
///   bracket, joined by <c>;</c> — the way FileMaker Pro shows them.</item>
///   <item><see cref="DisplayMode.Augmented"/> nodes — values FileMaker's lossy
///   UI hides but the XML carries — each render as their own trailing
///   <c>[HrLabel: value]</c> group, so invented syntax is visually distinct
///   from FileMaker-native display.</item>
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
        var natives = new List<string>();
        var augmented = new List<(string label, string value)>();

        foreach (var node in meta.Shape)
        {
            if (node.Display == DisplayMode.Hidden) continue;

            var value = DisplayValue(step, node);
            if (string.IsNullOrEmpty(value)) continue;

            if (node.Display == DisplayMode.Augmented)
            {
                // Display suppression is keyed on DefaultValue alone, decoupled
                // from the XML-emission Optional flag: a child can be always
                // emitted in XML (e.g. Set Variable's Repetition) yet hidden in
                // the display line when it holds its default.
                if (node.DefaultValue is not null && value == node.DefaultValue) continue;
                augmented.Add((node.HrLabel ?? ElementLabel(node), value));
            }
            else
            {
                natives.Add(value);
            }
        }

        var sb = new StringBuilder(meta.Name);
        if (natives.Count > 0)
            sb.Append(" [ ").Append(string.Join(" ; ", natives)).Append(" ]");
        foreach (var (label, value) in augmented)
            sb.Append(" [").Append(label).Append(": ").Append(value).Append(']');
        return sb.ToString();
    }

    private static string? DisplayValue(object src, ShapeNode node) => node switch
    {
        AttributeNode a => Get(src, a.PocoProperty ?? a.AttrName)?.ToString(),
        BoolStateChild b => (bool)(Get(src, b.PocoProperty ?? b.Element) ?? false) ? "On" : "Off",
        EnumValueChild e => Get(src, e.PocoProperty ?? e.Element)?.ToString(),
        BareCalcChild => (Get(src, node.PocoProperty ?? "Calculation") as Calculation)?.Text,
        NamedCalcChild nc => (Get(src, nc.PocoProperty ?? nc.Element) as Calculation)?.Text,
        NamedTextChild nt => Get(src, nt.PocoProperty ?? nt.Element)?.ToString(),
        FieldChild f => (Get(src, f.PocoProperty ?? f.Element) as FieldRef)?.ToDisplayString(),
        NamedRefChild nr => (Get(src, nr.PocoProperty ?? nr.Element) as NamedRef)?.Name,
        ParametersList pl => FormatList(Get(src, pl.PocoProperty ?? pl.Wrapper)),
        // ValueTypeChild / VariantBlock / Passthrough have no uniform display
        // contribution yet; they are surfaced per-step as those steps migrate.
        _ => null,
    };

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
