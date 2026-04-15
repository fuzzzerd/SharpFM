using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// A single input-field slot in a Show Custom Dialog step. When the &lt;InputFields&gt;
/// container is present, FM Pro always emits three slots — unused slots carry the
/// degenerate empty <see cref="FieldRef"/> (<c>table=""</c>, <c>id="0"</c>, <c>name=""</c>)
/// with no Label. The <c>Field</c> inside an InputField carries an optional
/// <c>repetition</c> attribute distinct from Set Variable's <c>$var[rep]</c> syntax.
/// </summary>
public sealed record ShowCustomDialogInputField(
    FieldRef Target,
    Calculation? Label,
    bool UsePasswordCharacter,
    int? Repetition)
{
    public static ShowCustomDialogInputField EmptySlot() =>
        new(FieldRef.ForField("", 0, ""), null, false, null);

    public bool IsEmpty =>
        !Target.IsVariable
        && string.IsNullOrEmpty(Target.Table)
        && Target.Id == 0
        && string.IsNullOrEmpty(Target.Name);

    public XElement ToXml()
    {
        var inputField = new XElement("InputField",
            new XAttribute("UsePasswordCharacter", UsePasswordCharacter ? "True" : "False"));

        var fieldEl = Target.ToXml("Field");
        if (Repetition is { } rep)
            fieldEl.SetAttributeValue("repetition", rep);

        inputField.Add(fieldEl);

        if (Label is not null)
            inputField.Add(new XElement("Label", Label.ToXml()));

        return inputField;
    }

    public static ShowCustomDialogInputField FromXml(XElement element)
    {
        var password = element.Attribute("UsePasswordCharacter")?.Value == "True";
        var fieldEl = element.Element("Field");

        FieldRef target;
        int? repetition = null;
        if (fieldEl is not null)
        {
            target = FieldRef.FromXml(fieldEl);
            var repAttr = fieldEl.Attribute("repetition")?.Value;
            if (int.TryParse(repAttr, out var parsed)) repetition = parsed;
        }
        else
        {
            target = FieldRef.ForField("", 0, "");
        }

        var labelEl = element.Element("Label");
        var labelCalc = labelEl?.Element("Calculation");
        var label = labelCalc is not null ? Calculation.FromXml(labelCalc) : null;

        return new ShowCustomDialogInputField(target, label, password, repetition);
    }
}
