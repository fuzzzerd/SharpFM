using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GoToFieldStep : ScriptStep, IStepFactory
{
    public const int XmlId = 17;
    public const string XmlName = "Go to Field";

    public bool SelectPerform { get; set; }
    public FieldRef? Target { get; set; }

    private GoToFieldStep() : base(false) { }

    public GoToFieldStep(
        bool selectPerform = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        SelectPerform = selectPerform;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Go to Field [ " + "Select/perform: " + (SelectPerform ? "On" : "Off") + " ; " + (Target?.ToDisplayString() ?? "") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<GoToFieldStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool selectPerform_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Select/perform:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(15).Trim(); selectPerform_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        FieldRef target = FieldRef.ForField("", 0, "");
        foreach (var tok in tokens) { if (!tok.StartsWith("Select/perform:", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tok)) { target = FieldRef.FromDisplayToken(tok); break; } }
        return new GoToFieldStep(selectPerform_v, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-field.html",
        // Canonical 017-GoToField: unconfigured form is <SelectAll> only;
        // the configured variant (-2) adds <Field>, so it is Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "SelectPerform", HrLabel = "Select/perform", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new FieldChild("Field") { PocoProperty = "Target", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
