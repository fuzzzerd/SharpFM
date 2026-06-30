using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Emits a <c>&lt;Step&gt;</c> element from a typed POCO by walking its
/// declarative <see cref="StepMetadata.Shape"/>. Element order follows shape
/// order exactly, satisfying FileMaker's canonical element-order requirements.
/// Optional nodes whose bound value is empty/default are omitted — which is
/// what keeps SharpFM from emitting placeholder elements FileMaker does not
/// write for an unconfigured step. The shape owns the four silent-failure-mode
/// primitives (<see cref="NamedTextChild"/> for Set Variable's <c>Name</c>,
/// <see cref="ParametersList"/> for the JS <c>&lt;P&gt;</c> form,
/// <see cref="NamedCalcChild"/> for the OnTimer <c>Interval</c> wrapper), so no
/// migrated step can reintroduce them.
/// </summary>
public static class StepXmlRenderer
{
    public static XElement Render(ScriptStep step, StepMetadata meta)
    {
        var el = new XElement("Step",
            new XAttribute("enable", step.Enabled ? "True" : "False"),
            new XAttribute("id", meta.Id),
            new XAttribute("name", meta.Name));

        foreach (var node in meta.Shape)
            Emit(el, step, node);

        return el;
    }

    private static void Emit(XElement parent, object src, ShapeNode node)
    {
        switch (node)
        {
            case AttributeNode a:
            {
                var v = ShapeReflection.Get(src, a.PocoProperty ?? a.AttrName);
                if (v is null && a.Optional) return;
                parent.Add(new XAttribute(a.AttrName, Str(v)));
                return;
            }

            case BoolStateChild b:
            {
                var v = (bool)(ShapeReflection.Get(src, b.PocoProperty ?? b.Element) ?? false);
                parent.Add(new XElement(b.Element, new XAttribute("state", v ? "True" : "False")));
                return;
            }

            case EnumValueChild e:
            {
                var v = ShapeReflection.Get(src, e.PocoProperty ?? e.Element);
                if (e.Optional && IsBlank(v)) return;
                parent.Add(new XElement(e.Element, new XAttribute("value", Str(v))));
                return;
            }

            case BareCalcChild:
            {
                var c = (Calculation?)ShapeReflection.Get(src, node.PocoProperty ?? "Calculation");
                if (node.Optional && string.IsNullOrEmpty(c?.Text)) return;
                parent.Add((c ?? new Calculation("")).ToXml());
                return;
            }

            case NamedCalcChild nc:
            {
                var c = (Calculation?)ShapeReflection.Get(src, nc.PocoProperty ?? nc.Element);
                if (nc.Optional && string.IsNullOrEmpty(c?.Text)) return;
                parent.Add(new XElement(nc.Element, (c ?? new Calculation("")).ToXml()));
                return;
            }

            case NamedTextChild nt:
            {
                var s = Str(ShapeReflection.Get(src, nt.PocoProperty ?? nt.Element));
                if (nt.Optional && s.Length == 0) return;
                parent.Add(new XElement(nt.Element, s));
                return;
            }

            case FieldChild f:
            {
                var fr = (FieldRef?)ShapeReflection.Get(src, f.PocoProperty ?? f.Element);
                if (fr is null)
                {
                    if (!f.Optional) parent.Add(new XElement(f.Element));
                    return;
                }
                parent.Add(fr.ToXml(f.Element));
                return;
            }

            case NamedRefChild nr:
            {
                var r = (NamedRef?)ShapeReflection.Get(src, nr.PocoProperty ?? nr.Element);
                if (r is null)
                {
                    if (!nr.Optional) parent.Add(new XElement(nr.Element));
                    return;
                }
                parent.Add(r.ToXml(nr.Element));
                return;
            }

            case ValueTypeChild vt:
            {
                var v = ShapeReflection.Get(src, vt.PocoProperty ?? vt.Element);
                if (v is null) return;
                parent.Add(InvokeToXml(v, vt.Element));
                return;
            }

            case ParametersList pl:
            {
                var items = (ShapeReflection.Get(src, pl.PocoProperty ?? pl.Wrapper) as IEnumerable)?
                    .Cast<object>().ToList() ?? new List<object>();
                if (pl.Optional && items.Count == 0) return;
                var wrapper = new XElement(pl.Wrapper, new XAttribute("Count", items.Count));
                foreach (var item in items)
                {
                    wrapper.Add(item is Calculation c
                        ? new XElement(pl.Child, c.ToXml())
                        : new XElement(pl.Child, Str(item)));
                }
                parent.Add(wrapper);
                return;
            }

            case WrapperChild w:
            {
                var wrapper = new XElement(w.Element);
                foreach (var child in w.Children)
                    Emit(wrapper, src, child);
                parent.Add(wrapper);
                return;
            }

            case VariantBlock vb:
            {
                var v = ShapeReflection.Get(src, node.PocoProperty
                    ?? throw new InvalidOperationException("VariantBlock requires PocoProperty."));
                if (v is null) return;
                var match = vb.Cases.FirstOrDefault(cse => cse.WhenType.IsInstanceOfType(v))
                    ?? throw new InvalidOperationException(
                        $"VariantBlock has no case for runtime type {v.GetType().Name}.");
                foreach (var child in match.Children)
                    Emit(parent, v, child);
                return;
            }

            case Passthrough:
            {
                if (ShapeReflection.Get(src, node.PocoProperty ?? "Passthrough") is IEnumerable<XElement> extra)
                    foreach (var x in extra)
                        parent.Add(new XElement(x));
                return;
            }

            default:
                throw new NotSupportedException($"Unhandled shape node: {node.GetType().Name}");
        }
    }

    private static readonly Dictionary<Type, MethodInfo> _toXmlCache = new();

    private static XElement InvokeToXml(object value, string elementName)
    {
        MethodInfo method;
        lock (_toXmlCache)
        {
            if (!_toXmlCache.TryGetValue(value.GetType(), out method!))
            {
                var withName = value.GetType().GetMethod("ToXml", new[] { typeof(string) });
                var noArg = value.GetType().GetMethod("ToXml", Type.EmptyTypes);
                method = withName ?? noArg
                    ?? throw new InvalidOperationException(
                        $"Value type {value.GetType().Name} has no ToXml() method for ValueTypeChild.");
                _toXmlCache[value.GetType()] = method;
            }
        }
        var args = method.GetParameters().Length == 1 ? new object[] { elementName } : Array.Empty<object>();
        return (XElement)method.Invoke(value, args)!;
    }

    private static string Str(object? v) => v switch
    {
        null => "",
        bool b => b ? "True" : "False",
        _ => v.ToString() ?? "",
    };

    private static bool IsBlank(object? v) => v is null || (v is string s && s.Length == 0);
}
