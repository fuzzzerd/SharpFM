using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SortRecordsByFieldStep : ScriptStep, IStepFactory
{
    public const int XmlId = 154;
    public const string XmlName = "Sort Records by Field";

    public string SortOrder { get; set; }
    public FieldRef? Field { get; set; }

    private SortRecordsByFieldStep() : base(false)
    {
        SortOrder = "SortAscending";
    }

    public SortRecordsByFieldStep(string sortOrder = "SortAscending", FieldRef? field = null, bool enabled = true)
        : base(enabled)
    {
        SortOrder = sortOrder;
        Field = field;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var order = SortOrder switch
        {
            "SortAscending" => "Ascending",
            "SortDescending" => "Descending",
            "SortValueList" => "Associated value list",
            _ => SortOrder,
        };
        return Field is null
            ? $"Sort Records by Field [ {order} ]"
            : $"Sort Records by Field [ {order} ; {Field.ToDisplayString()} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SortRecordsByFieldStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        string order = "SortAscending";
        FieldRef? field = null;
        bool orderSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (!orderSeen)
            {
                order = t switch
                {
                    "Ascending" => "SortAscending",
                    "Descending" => "SortDescending",
                    "Associated value list" => "SortValueList",
                    _ => t,
                };
                orderSeen = true;
            }
            else if (!string.IsNullOrWhiteSpace(t))
            {
                field = FieldRef.FromDisplayToken(t);
            }
        }
        return new SortRecordsByFieldStep(order, field, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/sort-records-by-field.html",
        Shape =
        [
            new EnumValueChild("SortRecordsByField") { PocoProperty = "SortOrder", HrLabel = "Sort order", DefaultValue = "SortAscending", ValidValues = ["SortAscending", "SortDescending", "SortValueList"], DisplayValues = ["Ascending", "Descending", "Associated value list"] },
            new FieldChild("Field") { PocoProperty = "Field", HrLabel = "Field", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
