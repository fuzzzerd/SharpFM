using System;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Applies one named HR-friendly param value onto a step POCO's shape-bound
/// public property, mutating the instance in place — the structured-caller
/// counterpart of <see cref="StepDisplayParser"/>. Where display parsing is
/// tolerant by contract (display text is user-edited), param application is
/// validating: unknown names, slots with no text representation, and
/// out-of-range values return an error instead of being silently dropped.
/// </summary>
internal static class StepParamApplier
{
    /// <summary>
    /// Resolves <paramref name="name"/> to a display slot (by bound property,
    /// XML element, or display label — see <see cref="ShapeHrView.MatchesName"/>)
    /// and assigns the converted value. Returns null on success, or a
    /// human-readable error message.
    /// </summary>
    public static string? Apply(ScriptStep target, StepMetadata meta, string name, string value)
    {
        var slot = ShapeHrView.HrNodes(meta.Shape).FirstOrDefault(n => ShapeHrView.MatchesName(n, name));
        if (slot is null)
            return $"Unknown param '{name}' for step '{meta.Name}'.";

        return Assign(target, meta, slot, name, value);
    }

    private static string? Assign(ScriptStep target, StepMetadata meta, ShapeNode node, string name, string value)
    {
        switch (node)
        {
            case BoolStateChild b:
                if (!TryParseOnOff(value, out var state))
                    return OnOffError(meta, name, value);
                ShapeReflection.Set(target, b.PocoProperty ?? b.Element, state != b.DisplayInverted);
                return null;

            case FlagChild f:
                if (!TryParseOnOff(value, out var present))
                    return OnOffError(meta, name, value);
                ShapeReflection.Set(target, f.PocoProperty ?? f.Element, present != f.DisplayInverted);
                return null;

            case EnumValueChild e:
                var wire = StepDisplayParser.ToWireValue(e, value);
                if (e.ValidValues is { Count: > 0 } valid && !valid.Contains(wire, StringComparer.OrdinalIgnoreCase))
                    return $"Param '{name}' of step '{meta.Name}' must be one of: " +
                        $"{string.Join(", ", ShapeHrView.DisplayValuesOf(e))}. Got '{value}'.";
                ShapeReflection.Set(target, e.PocoProperty ?? e.Element, wire);
                return null;

            case BareCalcChild:
                ShapeReflection.Set(target, node.PocoProperty ?? "Calculation", new Calculation(value));
                return null;

            case NamedCalcChild nc:
                ShapeReflection.Set(target, nc.PocoProperty ?? nc.Element, new Calculation(value));
                return null;

            case NamedTextChild nt:
                ShapeReflection.Set(target, nt.PocoProperty ?? nt.Element, value);
                return null;

            case FieldChild f:
                if (value.Length == 0)
                    return $"Param '{name}' of step '{meta.Name}' requires a field reference " +
                        "(e.g. \"Table::Field\" or \"$variable\").";
                ShapeReflection.Set(target, f.PocoProperty ?? f.Element, FieldRef.FromDisplayToken(value));
                return null;

            case NamedRefChild nr:
                // The text form carries the name only; id 0 is the unknown sentinel.
                ShapeReflection.Set(target, nr.PocoProperty ?? nr.Element, new NamedRef(0, value));
                return null;

            // HrOnly slots carry no XML binding of their own, and variant /
            // value-type / list slots have no generic text form. A step that
            // supports text values for these overrides ScriptStep.ApplyParam.
            default:
                return $"Param '{name}' of step '{meta.Name}' cannot be set from a text value; " +
                    "edit the step XML instead.";
        }
    }

    private static string OnOffError(StepMetadata meta, string name, string value) =>
        $"Param '{name}' of step '{meta.Name}' must be 'On' or 'Off' (got '{value}').";

    private static bool TryParseOnOff(string value, out bool on)
    {
        on = value.Equals("On", StringComparison.OrdinalIgnoreCase);
        return on || value.Equals("Off", StringComparison.OrdinalIgnoreCase);
    }
}
