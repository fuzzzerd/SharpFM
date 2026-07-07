using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Constructs a typed POCO from a <c>&lt;Step&gt;</c> element by walking its
/// declarative <see cref="StepMetadata.Shape"/> — the inverse of
/// <see cref="StepXmlRenderer"/>. The POCO is created through a parameterless
/// constructor (public or non-public) and its shape-bound properties are
/// populated by reflection, so a migrated step's <c>FromXml</c> reduces to a
/// single call.
/// </summary>
public static class StepXmlParser
{
    public static T Parse<T>(XElement step, StepMetadata meta) where T : ScriptStep =>
        (T)Parse(typeof(T), step, meta);

    public static ScriptStep Parse(Type pocoType, XElement step, StepMetadata meta)
    {
        if (Activator.CreateInstance(pocoType, nonPublic: true) is not ScriptStep instance)
            throw new InvalidOperationException(
                $"{pocoType.Name} needs a parameterless constructor for shape-driven parsing.");

        instance.Enabled = step.Attribute("enable")?.Value != "False";
        foreach (var node in meta.Shape)
            Populate(instance, step, node);

        CapturePassthrough(instance, step, meta.Shape);
        return instance;
    }

    /// <summary>
    /// A Passthrough slot captures every child element its sibling nodes did
    /// not model, so partially-known steps round-trip unknown children
    /// verbatim. The bound property may be a <c>List&lt;XElement&gt;</c> or a
    /// <see cref="StepChildBag"/>.
    /// </summary>
    private static void CapturePassthrough(object target, XElement parent, IReadOnlyList<ShapeNode> shape)
    {
        if (shape.OfType<Passthrough>().FirstOrDefault() is not { } pt) return;

        var modeled = shape.SelectMany(StepXmlValidator.ElementNamesOf).ToHashSet();
        var extras = parent.Elements()
            .Where(e => !modeled.Contains(e.Name.LocalName))
            .Select(e => new XElement(e))
            .ToList();
        var prop = pt.PocoProperty ?? "Passthrough";
        Set(target, prop,
            typeof(StepChildBag).IsAssignableFrom(ShapeReflection.PropertyType(target, prop))
                ? new StepChildBag(extras)
                : extras);
    }

    private static void Populate(object target, XElement step, ShapeNode node)
    {
        switch (node)
        {
            case AttributeNode a:
                Set(target, a.PocoProperty ?? a.AttrName, step.Attribute(a.AttrName)?.Value ?? a.DefaultValue ?? "");
                return;

            case BoolStateChild b:
            {
                // Present -> the bool. Absent: false for a required flag; for an
                // optional flag leave the POCO default (null for a bool? — keeps
                // "absent" distinct from "present and False").
                var el = step.Element(b.Element);
                if (el is not null) Set(target, b.PocoProperty ?? b.Element, el.Attribute(b.Attr)?.Value == "True");
                else if (!b.Optional) Set(target, b.PocoProperty ?? b.Element, false);
                return;
            }

            case EnumValueChild e:
            {
                var v = step.Element(e.Element)?.Attribute("value")?.Value ?? e.DefaultValue ?? "";
                Set(target, e.PocoProperty ?? e.Element, v);
                return;
            }

            case FlagChild fl:
                Set(target, fl.PocoProperty ?? fl.Element, step.Element(fl.Element) is not null);
                return;

            case BareCalcChild:
            {
                var el = step.Element("Calculation");
                if (el is not null) Set(target, node.PocoProperty ?? "Calculation", Calculation.FromXml(el));
                else if (!node.Optional) Set(target, node.PocoProperty ?? "Calculation", new Calculation(node.DefaultValue ?? ""));
                return;
            }

            case NamedCalcChild nc:
            {
                var el = step.Element(nc.Element)?.Element("Calculation");
                if (el is not null) Set(target, nc.PocoProperty ?? nc.Element, Calculation.FromXml(el));
                else if (!nc.Optional) Set(target, nc.PocoProperty ?? nc.Element, new Calculation(nc.DefaultValue ?? ""));
                return;
            }

            case NamedTextChild nt:
            {
                var el = step.Element(nt.Element);
                Set(target, nt.PocoProperty ?? nt.Element, el?.Value ?? "");
                if (nt.Attr is not null)
                    Set(target, nt.AttrProperty ?? nt.Attr,
                        el?.Attribute(nt.Attr)?.Value ?? nt.AttrDefault ?? "");
                return;
            }

            case FieldChild f:
            {
                var el = step.Element(f.Element);
                // A present-but-empty <Field/> means "no field bound" -> null;
                // an absent element leaves the POCO default untouched.
                if (el is not null)
                    Set(target, f.PocoProperty ?? f.Element,
                        el.HasAttributes || !string.IsNullOrEmpty(el.Value) ? FieldRef.FromXml(el) : null);
                return;
            }

            case NamedRefChild nr:
            {
                // Absent leaves the POCO default (e.g. Go to Related Record's
                // always-present Table keeps its empty default; an optional
                // Layout stays null).
                var el = step.Element(nr.Element);
                if (el is not null) Set(target, nr.PocoProperty ?? nr.Element, NamedRef.FromXml(el));
                return;
            }

            case ValueTypeChild vt:
            {
                // Leave the POCO's default (set in its parameterless ctor) when
                // the element is absent, rather than nulling a non-nullable
                // value-type property.
                var el = step.Element(vt.Element);
                if (el is not null) Set(target, vt.PocoProperty ?? vt.Element, InvokeFromXml(vt, target, el));
                return;
            }

            case ParametersList pl:
            {
                var prop = pl.PocoProperty ?? pl.Wrapper;
                var wrapper = step.Element(pl.Wrapper);
                // Item typing follows the bound property: List<Calculation>
                // round-trips the renderer's <P><Calculation> form; anything
                // else parses as plain strings.
                var itemType = ShapeReflection.PropertyType(target, prop)
                    .GetGenericArguments().FirstOrDefault();
                if (itemType == typeof(Calculation))
                {
                    var calcs = wrapper?.Elements(pl.Child)
                        .Select(p => p.Element("Calculation") is { } c
                            ? Calculation.FromXml(c)
                            : new Calculation(p.Value))
                        .ToList() ?? new List<Calculation>();
                    Set(target, prop, calcs);
                }
                else
                {
                    var items = wrapper?.Elements(pl.Child).Select(p => p.Value).ToList() ?? new List<string>();
                    Set(target, prop, items);
                }
                return;
            }

            case WrapperChild w:
            {
                var wrapper = step.Element(w.Element);
                if (wrapper is not null)
                {
                    foreach (var child in w.Children)
                        Populate(target, wrapper, child);
                    CapturePassthrough(target, wrapper, w.Children);
                }
                return;
            }

            case HrOnly:
                return; // display-grammar slot; no wire form of its own

            case Passthrough:
                return; // handled after the loop in Parse, with full-shape context

            case VariantBlock vb:
            {
                var prop = node.PocoProperty
                    ?? throw new InvalidOperationException("VariantBlock requires PocoProperty.");
                var match = vb.Cases.FirstOrDefault(c => CaseMatches(c, step));
                if (match is null) return; // leave the POCO's constructor default

                // Union cases are records with positional parameters, so there
                // is no parameterless constructor to call — create the blank
                // instance uninitialized and let the case's children populate
                // its init-only positional properties.
                var value = CreateBlank(match.WhenType);
                foreach (var child in match.Children)
                    Populate(value, step, child);
                Set(target, prop, value);
                return;
            }

            default:
                throw new NotSupportedException($"Unhandled shape node: {node.GetType().Name}");
        }
    }

    private static bool CaseMatches(VariantCase c, XElement step)
    {
        if (c.MatchElement is null) return true; // unconditional fallback
        var el = step.Element(c.MatchElement);
        if (el is null) return false;
        return c.MatchValues is null
            || c.MatchValues.Contains(el.Attribute("value")?.Value ?? "", StringComparer.Ordinal);
    }

    private static object CreateBlank(Type type) =>
        type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes)
            ?.Invoke(null)
        ?? System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);

    private static readonly Dictionary<(Type, string), MethodInfo> _fromXmlCache = new();

    private static object InvokeFromXml(ValueTypeChild vt, object target, XElement el)
    {
        // The value type's property declares its concrete type; reflect its
        // static FromXml(XElement) and invoke it.
        var propType = target.GetType().GetProperty(vt.PocoProperty ?? vt.Element)!.PropertyType;
        var underlying = Nullable.GetUnderlyingType(propType) ?? propType;
        MethodInfo method;
        lock (_fromXmlCache)
        {
            var key = (underlying, "FromXml");
            if (!_fromXmlCache.TryGetValue(key, out method!))
            {
                method = underlying.GetMethod("FromXml", BindingFlags.Public | BindingFlags.Static, new[] { typeof(XElement) })
                    ?? throw new InvalidOperationException(
                        $"Value type {underlying.Name} has no static FromXml(XElement) for ValueTypeChild.");
                _fromXmlCache[key] = method;
            }
        }
        return method.Invoke(null, new object[] { el })!;
    }

    private static void Set(object target, string prop, object? value) =>
        ShapeReflection.Set(target, prop, value);
}
