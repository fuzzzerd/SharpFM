using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// A single button slot in a Show Custom Dialog step. FM Pro always emits three
/// button slots; populated slots have a Label calculation, unused slots have
/// <c>Label == null</c>. CommitState is always present even on unused slots.
/// </summary>
public sealed record ShowCustomDialogButton(Calculation? Label, bool CommitState)
{
    public static ShowCustomDialogButton Empty(bool commitState = false) => new(null, commitState);

    public XElement ToXml()
    {
        var el = new XElement("Button",
            new XAttribute("CommitState", CommitState ? "True" : "False"));

        if (Label is not null)
            el.Add(Label.ToXml());

        return el;
    }

    public static ShowCustomDialogButton FromXml(XElement element)
    {
        var commit = element.Attribute("CommitState")?.Value == "True";
        var calcEl = element.Element("Calculation");
        var label = calcEl is not null ? Calculation.FromXml(calcEl) : null;
        return new ShowCustomDialogButton(label, commit);
    }
}
