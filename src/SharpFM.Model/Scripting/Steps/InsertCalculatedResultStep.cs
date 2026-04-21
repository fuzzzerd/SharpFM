using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert Calculated Result. The Target field uses the dual-format
/// fieldOrVariable shape: for a variable target, the XML contains a
/// sibling self-closing &lt;Text/&gt; element before &lt;Field&gt;$var&lt;/Field&gt;.
/// We preserve that marker by tracking whether the target is a variable
/// via the FieldRef's IsVariable flag and emitting the &lt;Text/&gt; sibling
/// accordingly.
/// </summary>
public sealed class InsertCalculatedResultStep : ScriptStep, IStepFactory
{
    public const int XmlId = 77;
    public const string XmlName = "Insert Calculated Result";

    public bool SelectAll { get; set; }
    public FieldRef? Target { get; set; }
    public Calculation Calculation { get; set; }

    public InsertCalculatedResultStep(
        bool selectAll = true,
        FieldRef? target = null,
        Calculation? calculation = null,
        bool enabled = true)
        : base(null, enabled)
    {
        SelectAll = selectAll;
        Target = target;
        Calculation = calculation ?? new Calculation("");
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("SelectAll", new XAttribute("state", SelectAll ? "True" : "False")),
            Calculation.ToXml("Calculation"));
        if (Target is not null)
        {
            if (Target.IsVariable) step.Add(new XElement("Text"));
            step.Add(Target.ToXml("Field"));
        }
        return step;
    }

    public override string ToDisplayLine()
    {
        var selectPart = SelectAll ? "Select ; " : "";
        var targetPart = Target is null ? "" : $"Target: {Target.ToDisplayString()} ; ";
        return $"Insert Calculated Result [ {selectPart}{targetPart}{Calculation.Text} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var selectAll = step.Element("SelectAll")?.Attribute("state")?.Value == "True";
        var calcEl = step.Element("Calculation");
        var calc = calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new InsertCalculatedResultStep(selectAll, target, calc, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool selectAll = false;
        FieldRef? target = null;
        Calculation calc = new("");
        bool calcSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Select", StringComparison.OrdinalIgnoreCase))
                selectAll = true;
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            else if (!calcSeen && !string.IsNullOrWhiteSpace(t))
            {
                calc = new Calculation(t);
                calcSeen = true;
            }
        }
        return new InsertCalculatedResultStep(selectAll, target, calc, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-calculated-result.html",
        Params =
        [
            new ParamMetadata { Name = "SelectAll", XmlElement = "SelectAll", XmlAttr = "state", Type = "boolean", HrLabel = "Select", ValidValues = ["On", "Off"], DefaultValue = "True" },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "fieldOrVariable", HrLabel = "Target" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
