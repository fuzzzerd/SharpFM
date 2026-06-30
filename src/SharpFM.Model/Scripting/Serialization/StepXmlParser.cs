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
        return instance;
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
                var state = step.Element(b.Element)?.Attribute("state")?.Value;
                Set(target, b.PocoProperty ?? b.Element, state == "True");
                return;
            }

            case EnumValueChild e:
            {
                var v = step.Element(e.Element)?.Attribute("value")?.Value ?? e.DefaultValue ?? "";
                Set(target, e.PocoProperty ?? e.Element, v);
                return;
            }

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
                Set(target, nt.PocoProperty ?? nt.Element, step.Element(nt.Element)?.Value ?? "");
                return;

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
                var wrapper = step.Element(pl.Wrapper);
                var items = wrapper?.Elements(pl.Child).Select(p => p.Value).ToList() ?? new List<string>();
                Set(target, pl.PocoProperty ?? pl.Wrapper, items);
                return;
            }

            case WrapperChild w:
            {
                var wrapper = step.Element(w.Element);
                if (wrapper is not null)
                    foreach (var child in w.Children)
                        Populate(target, wrapper, child);
                return;
            }

            case Passthrough:
            case VariantBlock:
                throw new NotSupportedException(
                    $"Shape node {node.GetType().Name} parsing is not yet implemented; " +
                    "it will be added when the first step that needs it is migrated.");

            default:
                throw new NotSupportedException($"Unhandled shape node: {node.GetType().Name}");
        }
    }

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
