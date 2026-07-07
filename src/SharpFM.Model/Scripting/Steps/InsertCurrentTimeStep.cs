using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InsertCurrentTimeStep : ScriptStep, IStepFactory
{
    public const int XmlId = 14;
    public const string XmlName = "Insert Current Time";

    public bool Select { get; set; }
    public FieldRef? Target { get; set; }

    private InsertCurrentTimeStep() : base(false) { }

    public InsertCurrentTimeStep(
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
        "Insert Current Time [ " + "Select: " + (Select ? "On" : "Off") + " ; " + "Target: " + (Target?.ToDisplayString() ?? "") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertCurrentTimeStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool select_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Select:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); select_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        FieldRef? target = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Target:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); if (v.Length > 0) target = FieldRef.FromDisplayToken(v); break; } }
        return new InsertCurrentTimeStep(select_v, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-current-time.html",
        // Canonical 014-InsertCurrentTime: only <SelectAll>; <Field> is omitted
        // until a target is bound, so it is Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Target", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
