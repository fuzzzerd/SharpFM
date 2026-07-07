using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Constructs a typed POCO from parsed display-text tokens by walking the
/// same <see cref="StepMetadata.Shape"/> display slots that
/// <see cref="StepDisplayRenderer"/> renders — the display-side counterpart of
/// <see cref="StepXmlParser"/>. Labeled tokens bind by <c>HrLabel</c>, a bare
/// token equal to a flag's label marks presence, and remaining tokens bind
/// positionally to unlabeled slots in display order. Unrecognized tokens are
/// ignored (display text is user-edited; tolerance is the contract).
///
/// <para>
/// A step is eligible only when its display grammar is faithful — the
/// display round-trip contract suite is the proof. Steps with variant,
/// value-type, or list slots keep hand-written display parsing.
/// </para>
/// </summary>
public static class StepDisplayParser
{
    public static T Parse<T>(bool enabled, string[] hrParams, StepMetadata meta) where T : ScriptStep =>
        (T)Parse(typeof(T), enabled, hrParams, meta);

    public static ScriptStep Parse(Type pocoType, bool enabled, string[] hrParams, StepMetadata meta)
    {
        if (Activator.CreateInstance(pocoType, nonPublic: true) is not ScriptStep instance)
            throw new InvalidOperationException(
                $"{pocoType.Name} needs a parameterless constructor for shape-driven display parsing.");
        instance.Enabled = enabled;

        var slots = ShapeHrView.HrNodes(meta.Shape).Where(n => n is not HrOnly).ToList();
        var used = new bool[slots.Count];

        foreach (var raw in hrParams)
        {
            var token = raw.Trim();

            // Labeled: "HrLabel: value"
            var bound = false;
            for (int i = 0; i < slots.Count && !bound; i++)
            {
                var label = slots[i].HrLabel;
                if (used[i] || label is null) continue;
                if (!token.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase)) continue;
                used[i] = true;
                Assign(instance, slots[i], token[(label.Length + 1)..].Trim());
                bound = true;
            }
            if (bound) continue;

            // Bare presence flag: token equals a flag slot's label
            for (int i = 0; i < slots.Count && !bound; i++)
            {
                if (used[i] || slots[i] is not FlagChild flag) continue;
                var label = slots[i].HrLabel ?? flag.Element;
                if (!token.Equals(label, StringComparison.OrdinalIgnoreCase)) continue;
                used[i] = true;
                ShapeReflection.Set(instance, flag.PocoProperty ?? flag.Element, true);
                bound = true;
            }
            if (bound) continue;

            // Positional: next unused unlabeled slot
            for (int i = 0; i < slots.Count && !bound; i++)
            {
                if (used[i] || slots[i].HrLabel is not null) continue;
                used[i] = true;
                Assign(instance, slots[i], token);
                bound = true;
            }
        }

        return instance;
    }

    private static void Assign(object target, ShapeNode node, string value)
    {
        // The empty-slot / fallback token carries no information beyond the
        // constructor default; an empty value likewise leaves the default.
        if (value.Length == 0 || value == node.DisplayEmptyAs) return;

        switch (node)
        {
            case BoolStateChild b:
                ShapeReflection.Set(target, b.PocoProperty ?? b.Element,
                    value.Equals("On", StringComparison.OrdinalIgnoreCase) != b.DisplayInverted);
                return;

            case EnumValueChild e:
                ShapeReflection.Set(target, e.PocoProperty ?? e.Element, ToWireValue(e, value));
                return;

            case BareCalcChild:
                ShapeReflection.Set(target, node.PocoProperty ?? "Calculation", new Calculation(value));
                return;

            case NamedCalcChild nc:
                ShapeReflection.Set(target, nc.PocoProperty ?? nc.Element, new Calculation(value));
                return;

            case NamedTextChild nt:
                ShapeReflection.Set(target, nt.PocoProperty ?? nt.Element, value);
                return;

            case FieldChild f:
                ShapeReflection.Set(target, f.PocoProperty ?? f.Element, FieldRef.FromDisplayToken(value));
                return;

            case NamedRefChild nr:
                // Display carries the name only; id 0 is the unknown sentinel.
                ShapeReflection.Set(target, nr.PocoProperty ?? nr.Element, new NamedRef(0, value));
                return;

            default:
                return; // variant/value-type/list slots need hand-written parsing
        }
    }

    /// <summary>Inverse of the renderer's wire→display translation.</summary>
    private static string ToWireValue(ShapeNode node, string display)
    {
        if (node.DisplayValues is null || node.ValidValues is null) return display;
        var i = node.DisplayValues.ToList().FindIndex(v => v.Equals(display, StringComparison.OrdinalIgnoreCase));
        return i >= 0 && i < node.ValidValues.Count ? node.ValidValues[i] : display;
    }
}
