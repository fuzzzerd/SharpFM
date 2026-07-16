using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Applies one named HR-friendly param value onto a step POCO's shape-bound
/// public property, mutating the instance in place — the structured-caller
/// counterpart of <see cref="StepDisplayParser"/>. Where display parsing is
/// tolerant by contract (display text is user-edited), param application is
/// validating: unknown names, unrecognized values, and out-of-range values
/// return an error instead of being silently dropped.
///
/// <para>
/// Slots the typed conversion cannot set — <see cref="HrOnly"/>, variant /
/// value-type / list slots, and slots bound to emit-only projection
/// properties — route through the step's own display-token grammar via
/// <see cref="ApplyViaDisplayGrammar"/>, so any value a step can parse from
/// its display line is also settable as a param, with no per-step wiring.
/// </para>
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
        // Several shapes declare an HrOnly display alias next to the slot
        // that actually binds the property (e.g. Insert Audio/Video's
        // UniversalPathList), so prefer a typed-assignable match over the
        // first name match.
        var matches = ShapeHrView.HrNodes(meta.Shape).Where(n => ShapeHrView.MatchesName(n, name)).ToList();
        if (matches.Count == 0)
            return $"Unknown param '{name}' for step '{meta.Name}'.";

        var slot = matches.FirstOrDefault(n => IsTypedAssignable(target, n)) ?? matches[0];
        return Assign(target, meta, slot, name, value);
    }

    /// <summary>
    /// True when the typed conversion in <see cref="Assign"/> can set this
    /// slot directly: a convertible node kind whose bound property is
    /// writable. Emit-only projections (e.g. Perform Script's
    /// ParameterBeforeScript) and enums whose display values have no paired
    /// wire values are excluded — they route through the display grammar,
    /// which reaches the real storage.
    /// </summary>
    private static bool IsTypedAssignable(ScriptStep target, ShapeNode node) => node switch
    {
        BoolStateChild b => ShapeReflection.CanWrite(target, b.PocoProperty ?? b.Element),
        FlagChild f => ShapeReflection.CanWrite(target, f.PocoProperty ?? f.Element),
        // Display→wire translation pairs DisplayValues with ValidValues by
        // index; DisplayValues alone leaves the wire form unknowable.
        EnumValueChild e => ShapeReflection.CanWrite(target, e.PocoProperty ?? e.Element)
            && !(e.DisplayValues is { Count: > 0 } && e.ValidValues is not { Count: > 0 }),
        BareCalcChild => ShapeReflection.CanWrite(target, node.PocoProperty ?? "Calculation"),
        NamedCalcChild nc => ShapeReflection.CanWrite(target, nc.PocoProperty ?? nc.Element),
        NamedTextChild nt => ShapeReflection.CanWrite(target, nt.PocoProperty ?? nt.Element),
        FieldChild f => ShapeReflection.CanWrite(target, f.PocoProperty ?? f.Element),
        NamedRefChild nr => ShapeReflection.CanWrite(target, nr.PocoProperty ?? nr.Element),
        _ => false,
    };

    private static string? Assign(ScriptStep target, StepMetadata meta, ShapeNode node, string name, string value)
    {
        if (!IsTypedAssignable(target, node))
            return ApplyViaDisplayGrammar(target, meta, node, name, value);

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

            default:
                return ApplyViaDisplayGrammar(target, meta, node, name, value);
        }
    }

    /// <summary>
    /// Routes a value through the step's own display-token grammar. The
    /// synthesized token is validated against a blank probe first: it must
    /// produce a wire-level change there, otherwise the grammar did not
    /// recognize it and re-parsing could silently reset state. The accepted
    /// token is then re-parsed onto the live instance together with the
    /// step's current display tokens — those re-assert the display-visible
    /// state, and properties display text does not carry stay untouched.
    /// Current tokens carrying the same label are dropped so the new token
    /// wins under both first-match and last-match parser styles; raw tokens
    /// are appended, which the raw-form parsers resolve last-wins.
    /// </summary>
    private static string? ApplyViaDisplayGrammar(ScriptStep target, StepMetadata meta, ShapeNode node, string name, string value)
    {
        var blank = StepDisplayFactory.TryCreate(meta.Name, true, []);
        if (blank is null)
            return $"No typed POCO factory registered for '{meta.Name}'.";
        var blankXml = blank.ToXml().ToString();

        foreach (var (token, labelPrefix) in CandidateTokens(target, node, value))
        {
            var probe = StepDisplayFactory.TryCreate(meta.Name, true, [token]);
            if (probe is null || probe.ToXml().ToString() == blankXml) continue;

            IEnumerable<string> retained = ScriptLineParser.ParseLine(target.ToDisplayLine()).Params;
            if (labelPrefix is not null)
                retained = retained.Where(t => !t.TrimStart().StartsWith(labelPrefix, StringComparison.OrdinalIgnoreCase));
            var current = retained.ToList();

            // The value is already in the display line — nothing to change.
            if (current.Any(t => t.Trim().Equals(token, StringComparison.OrdinalIgnoreCase)))
                return null;

            // Parsers differ in whether the first or the last matching token
            // wins, so try the new token in both positions and require a
            // wire-level change. A no-change re-parse only re-assigns the
            // display-visible state to itself, so the failed order is
            // harmless.
            var before = target.ToXml().ToString();

            target.PopulateFromDisplay([.. current, token]);
            if (target.ToXml().ToString() != before) return null;

            target.PopulateFromDisplay([token, .. current]);
            if (target.ToXml().ToString() != before) return null;
        }

        return $"Param '{name}' of step '{meta.Name}' did not accept value '{value}'; " +
            "it may match the step's default. Edit the step XML for exact control.";
    }

    // A raw (unlabeled) token is only meaningful to a hand-written display
    // parser, which recognizes tokens by form; the shape-driven parser would
    // bind it to the first unused positional slot — the wrong one. For a
    // slot with no HrLabel the raw form is canonical and goes first (a
    // free-text grammar would swallow a name-prefixed token verbatim); the
    // name-prefixed form is the rescue for grammars keyed on the slot name
    // (e.g. Show Custom Dialog's "Buttons:").
    private static IEnumerable<(string Token, string? LabelPrefix)> CandidateTokens(ScriptStep target, ShapeNode node, string value)
    {
        var handParser = HasHandWrittenDisplayParser(target.GetType());
        var label = node.HrLabel ?? ShapeHrView.NameOf(node);

        if (node.HrLabel is null && handParser)
            yield return (value, null);

        if (label.Length > 0)
            yield return ($"{label}: {value}", $"{label}:");

        if (node.HrLabel is not null && handParser)
            yield return (value, null);
    }

    private static readonly ConcurrentDictionary<Type, bool> _handDisplayParser = new();

    /// <summary>
    /// True when the step overrides <see cref="ScriptStep.PopulateFromDisplay"/>
    /// itself rather than inheriting the shape-driven default declared on the
    /// generic <c>ScriptStep&lt;TSelf&gt;</c> base.
    /// </summary>
    private static bool HasHandWrittenDisplayParser(Type stepType) =>
        _handDisplayParser.GetOrAdd(stepType, t =>
            t.GetMethod(nameof(ScriptStep.PopulateFromDisplay), BindingFlags.NonPublic | BindingFlags.Instance)
                ?.DeclaringType?.IsGenericType == false);

    private static string OnOffError(StepMetadata meta, string name, string value) =>
        $"Param '{name}' of step '{meta.Name}' must be 'On' or 'Off' (got '{value}').";

    private static bool TryParseOnOff(string value, out bool on)
    {
        on = value.Equals("On", StringComparison.OrdinalIgnoreCase);
        return on || value.Equals("Off", StringComparison.OrdinalIgnoreCase);
    }
}
