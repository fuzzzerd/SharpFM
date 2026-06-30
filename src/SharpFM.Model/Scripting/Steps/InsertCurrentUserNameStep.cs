using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InsertCurrentUserNameStep : ScriptStep, IStepFactory
{
    public const int XmlId = 60;
    public const string XmlName = "Insert Current User Name";

    public bool Select { get; set; }
    public FieldRef? Target { get; set; }

    private InsertCurrentUserNameStep() : base(false) { }

    public InsertCurrentUserNameStep(
        bool select = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        Select = select;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Insert Current User Name [ " + "Select: " + (Select ? "On" : "Off") + " ; " + "Target: " + (Target?.ToDisplayString() ?? "") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertCurrentUserNameStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool select_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Select:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); select_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        FieldRef target = FieldRef.ForField("", 0, "");
        foreach (var tok in tokens) { if (tok.StartsWith("Target:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); target = FieldRef.FromDisplayToken(v); break; } }
        return new InsertCurrentUserNameStep(select_v, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-current-user-name.html",
        // Canonical 060-InsertCurrentUserName: only <SelectAll>; <Field> is
        // omitted until a target is bound, so it is Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Target", Optional = true },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "SelectAll",
                XmlElement = "SelectAll",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Select",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
            },
            new ParamMetadata
            {
                Name = "Field",
                XmlElement = "Field",
                Type = "fieldOrVariable",
                HrLabel = "Target",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
