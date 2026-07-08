using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Lints a <c>&lt;Step&gt;</c> element against its declared
/// <see cref="StepMetadata.Shape"/>: every child element must be one the shape
/// can emit, and the children must appear in shape order. This catches the
/// element-order class of FileMaker paste failures (e.g. a <c>&lt;Name&gt;</c>
/// that is present but not last in Set Variable) before XML reaches the
/// clipboard. Optional children may be absent; unknown trailing children are
/// permitted only when the shape declares a <see cref="Passthrough"/> slot.
/// </summary>
public static class StepXmlValidator
{
    public static IReadOnlyList<string> Validate(XElement step, StepMetadata meta)
    {
        var issues = new List<string>();
        var expected = ExpectedElementOrder(meta.Shape);
        var allowsPassthrough = meta.Shape.Any(n => n is Passthrough);

        int cursor = 0;
        foreach (var child in step.Elements())
        {
            var name = child.Name.LocalName;
            var found = false;
            for (int i = cursor; i < expected.Count; i++)
            {
                if (expected[i] == name) { cursor = i + 1; found = true; break; }
            }
            if (!found)
            {
                if (allowsPassthrough && !expected.Contains(name)) continue; // unmodeled child preserved verbatim
                issues.Add(expected.Contains(name)
                    ? $"<{name}> is out of canonical order for step '{meta.Name}'."
                    : $"<{name}> is not a recognized child of step '{meta.Name}'.");
            }
        }
        return issues;
    }

    /// <summary>
    /// The ordered element names the shape can emit. <see cref="AttributeNode"/>
    /// contributes nothing (it is a Step-level attribute). A
    /// <see cref="VariantBlock"/> contributes its cases' element names in order
    /// (any case may be the one present).
    /// </summary>
    private static IReadOnlyList<string> ExpectedElementOrder(IReadOnlyList<ShapeNode> shape)
    {
        var names = new List<string>();
        foreach (var node in shape)
            names.AddRange(ElementNamesOf(node));
        return names;
    }

    /// <summary>The XML element names a single shape node can emit (empty for attribute/step-level nodes).</summary>
    public static IEnumerable<string> ElementNamesOf(ShapeNode node) => node switch
    {
        AttributeNode or Passthrough => [],
        BoolStateChild b => [b.Element],
        EnumValueChild e => [e.Element],
        FlagChild fl => [fl.Element],
        BareCalcChild => ["Calculation"],
        NamedCalcChild nc => [nc.Element],
        NamedTextChild nt => [nt.Element],
        FieldChild f => [f.Element],
        NamedRefChild nr => [nr.Element],
        ValueTypeChild vt => [vt.Element],
        ParametersList pl => [pl.Wrapper],
        WrapperChild w => [w.Element],
        VariantBlock vb => vb.Cases.SelectMany(c => c.Children.SelectMany(ElementNamesOf)),
        _ => [],
    };
}
