using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InsertFromLastVisitedStep : ScriptStep, IStepFactory
{
    public const int XmlId = 12;
    public const string XmlName = "Insert from Last Visited";

    public bool Select { get; set; }
    public FieldRef? Target { get; set; }

    private InsertFromLastVisitedStep() : base(false) { Select = true; }

    public InsertFromLastVisitedStep(
        bool select = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        Select = select;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertFromLastVisitedStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<InsertFromLastVisitedStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-from-last-visited.html",
        // Canonical 012-InsertFromLastVisited: only <SelectAll>; <Field> is
        // omitted until a target is bound, so it is Optional.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new FieldChild("Field") { PocoProperty = "Target", Optional = true, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
