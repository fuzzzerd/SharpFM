using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SortRecordsByFieldStep : ScriptStep, IStepFactory
{
    public const int XmlId = 154;
    public const string XmlName = "Sort Records by Field";

    public string SortOrder { get; set; }
    public FieldRef? Field { get; set; }

    public SortRecordsByFieldStep(string sortOrder = "SortAscending", FieldRef? field = null, bool enabled = true)
        : base(enabled)
    {
        SortOrder = sortOrder;
        Field = field;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("SortRecordsByField", new XAttribute("value", SortOrder)));
        if (Field is not null) step.Add(Field.ToXml("Field"));
        return step;
    }

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

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var order = step.Element("SortRecordsByField")?.Attribute("value")?.Value ?? "SortAscending";
        var fieldEl = step.Element("Field");
        var field = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new SortRecordsByFieldStep(order, field, enabled);
    }

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
        Params =
        [
            new ParamMetadata
            {
                Name = "SortRecordsByField",
                XmlElement = "SortRecordsByField",
                XmlAttr = "value",
                Type = "enum",
                HrLabel = "Sort order",
                ValidValues = ["Ascending", "Descending", "Associated value list"],
                DefaultValue = "SortAscending",
            },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Field" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
