using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Replace Field Contents. The <c>&lt;Restore state="True"/&gt;</c> element
/// is <b>intentionally dropped</b> per the zero-loss audit in
/// <c>docs/advanced-filemaker-scripting-syntax.md</c> — FM Pro never changes
/// the value and never emits the element in clipboard output, matching the
/// same drop pattern as <see cref="IfStep"/>.
/// </summary>
public sealed class ReplaceFieldContentsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 91;
    public const string XmlName = "Replace Field Contents";

    public bool WithDialog { get; set; }
    public FieldRef Field { get; set; }
    public string Mode { get; set; }
    public Calculation? Calculation { get; set; }
    public SerialNumberOptions? SerialOptions { get; set; }

    public ReplaceFieldContentsStep(
        bool withDialog = true,
        FieldRef? field = null,
        string mode = "Calculation",
        Calculation? calculation = null,
        SerialNumberOptions? serialOptions = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Field = field ?? FieldRef.ForField("", 0, "");
        Mode = mode;
        Calculation = calculation;
        SerialOptions = serialOptions;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("With", new XAttribute("value", Mode)));
        if (Calculation is not null) step.Add(Calculation.ToXml("Calculation"));
        if (SerialOptions is not null) step.Add(SerialOptions.ToXml());
        step.Add(Field.ToXml("Field"));
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"With dialog: {(WithDialog ? "On" : "Off")}",
            Field.ToDisplayString(),
        };
        var modePart = Mode switch
        {
            "CurrentContents" => "Current contents",
            "SerialNumbers" => "Serial numbers",
            "Calculation" => Calculation?.Text ?? "",
            _ => Mode,
        };
        parts.Add(modePart);
        if (SerialOptions is not null && !SerialOptions.PerformAutoEnter) parts.Add("Skip auto-enter options");
        return $"Replace Field Contents [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        var mode = step.Element("With")?.Attribute("value")?.Value ?? "Calculation";
        var calcEl = step.Element("Calculation");
        var calc = calcEl is not null ? Values.Calculation.FromXml(calcEl) : null;
        var snEl = step.Element("SerialNumbers");
        var sn = snEl is not null ? SerialNumberOptions.FromXml(snEl) : null;
        var fieldEl = step.Element("Field");
        var field = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, "");
        return new ReplaceFieldContentsStep(withDialog, field, mode, calc, sn, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool withDialog = true;
        FieldRef field = FieldRef.ForField("", 0, "");
        string mode = "Calculation";
        Calculation? calc = null;
        bool fieldSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (!fieldSeen && !string.IsNullOrWhiteSpace(t))
            {
                field = FieldRef.FromDisplayToken(t);
                fieldSeen = true;
            }
            else if (t.Equals("Current contents", StringComparison.OrdinalIgnoreCase))
                mode = "CurrentContents";
            else if (t.Equals("Serial numbers", StringComparison.OrdinalIgnoreCase))
                mode = "SerialNumbers";
            else if (!string.IsNullOrWhiteSpace(t))
            {
                mode = "Calculation";
                calc = new Calculation(t);
            }
        }
        return new ReplaceFieldContentsStep(withDialog, field, mode, calc, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/replace-field-contents.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog", ValidValues = ["On", "Off"] },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", Required = true },
            new ParamMetadata { Name = "With", XmlElement = "With", XmlAttr = "value", Type = "enum", ValidValues = ["Current contents", "Serial numbers", "Calculation"], DefaultValue = "Calculation" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation" },
            new ParamMetadata { Name = "SerialNumbers", XmlElement = "SerialNumbers", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
