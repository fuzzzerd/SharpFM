using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class FindMatchingRecordsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 155;
    public const string XmlName = "Find Matching Records";

    public string Mode { get; set; }
    public FieldRef? Field { get; set; }

    public FindMatchingRecordsStep(string mode = "FindMatchingReplace", FieldRef? field = null, bool enabled = true)
        : base(enabled)
    {
        Mode = mode;
        Field = field;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("FindMatchingRecordsByField", new XAttribute("value", Mode)));
        if (Field is not null) step.Add(Field.ToXml("Field"));
        return step;
    }

    public override string ToDisplayLine()
    {
        var mode = Mode switch
        {
            "FindMatchingReplace" or "Replace" => "Replace",
            "FindMatchingConstrain" or "Constrain" => "Constrain",
            "FindMatchingExtend" or "Extend" => "Extend",
            _ => Mode,
        };
        return Field is null
            ? $"Find Matching Records [ {mode} ]"
            : $"Find Matching Records [ {mode} ; {Field.ToDisplayString()} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var mode = step.Element("FindMatchingRecordsByField")?.Attribute("value")?.Value ?? "FindMatchingReplace";
        var fieldEl = step.Element("Field");
        var field = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new FindMatchingRecordsStep(mode, field, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        string mode = "FindMatchingReplace";
        FieldRef? field = null;
        bool modeSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (!modeSeen)
            {
                mode = t switch
                {
                    "Replace" => "FindMatchingReplace",
                    "Constrain" => "FindMatchingConstrain",
                    "Extend" => "FindMatchingExtend",
                    _ => t,
                };
                modeSeen = true;
            }
            else if (!string.IsNullOrWhiteSpace(t))
            {
                field = FieldRef.FromDisplayToken(t);
            }
        }
        return new FindMatchingRecordsStep(mode, field, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "found sets",
        HelpUrl = "https://help.claris.com/en/pro-help/content/find-matching-records.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "FindMatchingRecordsByField",
                XmlElement = "FindMatchingRecordsByField",
                XmlAttr = "value",
                Type = "enum",
                ValidValues = ["Replace", "Constrain", "Extend"],
                DefaultValue = "FindMatchingReplace",
            },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
