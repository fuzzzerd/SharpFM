using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// Ordered dictionary-bag of step children used by AI/ML steps (Configure
/// Local Notification, Generate Response from Model, Perform RAG Action,
/// Send Mail, etc.). Element names can repeat; order matters for FM Pro
/// round-trip fidelity, so we store an ordered list.
///
/// <para>
/// Typed POCOs that need hot-field access can index by element name
/// via <see cref="FirstByName"/>. The bag exists because FM keeps adding
/// params to these AI/ML steps faster than we can track; a loose bag
/// gives lossless round-trip without rigid modeling churn.
/// </para>
/// </summary>
public sealed class StepChildBag
{
    private readonly List<XElement> _children;

    public StepChildBag() { _children = new List<XElement>(); }
    public StepChildBag(IEnumerable<XElement> children) { _children = children.Select(c => new XElement(c)).ToList(); }

    public IReadOnlyList<XElement> Children => _children;

    public XElement? FirstByName(string name) => _children.FirstOrDefault(c => c.Name.LocalName == name);

    public IEnumerable<XElement> AllByName(string name) => _children.Where(c => c.Name.LocalName == name);

    public void AppendTo(XElement parent)
    {
        foreach (var c in _children) parent.Add(new XElement(c));
    }

    public static StepChildBag FromParent(XElement parent) => new(parent.Elements());
}
