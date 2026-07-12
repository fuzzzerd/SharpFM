using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SortRecordsByFieldStep : ScriptStep<SortRecordsByFieldStep>, IStepFactory
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

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/sort-records-by-field.html",
        Shape =
        [
            new EnumValueChild("SortRecordsByField") { PocoProperty = "SortOrder", DefaultValue = "SortAscending", ValidValues = ["SortAscending", "SortDescending", "SortValueList"], DisplayValues = ["Ascending", "Descending", "Associated value list"] },
            new FieldChild("Field") { PocoProperty = "Field", Optional = true },
        ],
    };
}
