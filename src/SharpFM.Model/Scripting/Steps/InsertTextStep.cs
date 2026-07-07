using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert Text carries a literal <c>&lt;Text&gt;...&lt;/Text&gt;</c>
/// (not a Calculation/CDATA block) and a dual-format Field target. Both the
/// Text content and the Field target are omitted from the unconfigured
/// canonical form, so each is Optional.
/// </summary>
public sealed class InsertTextStep : ScriptStep, IStepFactory
{
    public const int XmlId = 61;
    public const string XmlName = "Insert Text";

    public bool SelectAll { get; set; }
    public FieldRef? Target { get; set; }
    public string Text { get; set; }

    private InsertTextStep() : base(false) { Text = ""; }

    public InsertTextStep(bool selectAll = true, FieldRef? target = null, string text = "", bool enabled = true)
        : base(enabled)
    {
        SelectAll = selectAll;
        Target = target;
        Text = text;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var selectPart = SelectAll ? "Select ; " : "";
        var targetPart = Target is null ? "" : $"Target: {Target.ToDisplayString()} ; ";
        return $"Insert Text [ {selectPart}{targetPart}\"{Text}\" ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertTextStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool selectAll = false;
        FieldRef? target = null;
        string text = "";
        bool textSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Select", StringComparison.OrdinalIgnoreCase))
                selectAll = true;
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            else if (!textSeen && !string.IsNullOrWhiteSpace(t))
            {
                text = t;
                if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length >= 2)
                    text = text.Substring(1, text.Length - 2);
                textSeen = true;
            }
        }
        return new InsertTextStep(selectAll, target, text, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-text.html",
        // Canonical 061-InsertText: only <SelectAll>; both <Text> and <Field>
        // are omitted until configured, so both are Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "SelectAll", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new NamedTextChild("Text") { PocoProperty = "Text", Optional = true },
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Target", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
