using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class FindMatchingRecordsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 155;
    public const string XmlName = "Find Matching Records";

    public string Mode { get; set; } = "FindMatchingReplace";
    public FieldRef? Field { get; set; }

    private FindMatchingRecordsStep() : base(false) { }

    public FindMatchingRecordsStep(string mode = "FindMatchingReplace", FieldRef? field = null, bool enabled = true)
        : base(enabled)
    {
        Mode = mode;
        Field = field;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<FindMatchingRecordsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<FindMatchingRecordsStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/find-matching-records.html",
        // Canonical: the always-present mode enum, then the optional target Field.
        Shape =
        [
            new EnumValueChild("FindMatchingRecordsByField")
            {
                PocoProperty = "Mode",
                DefaultValue = "FindMatchingReplace",
                ValidValues = ["FindMatchingReplace", "FindMatchingConstrain", "FindMatchingExtend"],
                DisplayValues = ["Replace", "Constrain", "Extend"],
                Display = DisplayMode.Native,
            },
            new FieldChild("Field") { PocoProperty = "Field", Optional = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
