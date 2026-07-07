using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
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
    public Calculation? Calculation { get; set; }

    private InsertCalculatedResultStep() : base(false) { }

    public InsertCalculatedResultStep(
        bool selectAll = true,
        FieldRef? target = null,
        Calculation? calculation = null,
        bool enabled = true)
        : base(enabled)
    {
        SelectAll = selectAll;
        Target = target;
        Calculation = calculation;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: bare "Select" presence token from a BoolStateChild, which
    // always renders labeled On/Off.
    public override string ToDisplayLine()
    {
        var selectPart = SelectAll ? "Select ; " : "";
        var targetPart = Target is null ? "" : $"Target: {Target.ToDisplayString()} ; ";
        return $"Insert Calculated Result [ {selectPart}{targetPart}{Calculation?.Text ?? ""} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertCalculatedResultStep>(step, Metadata);

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
        // Canonical: SelectAll, then the optional calculation and field target.
        // The bare <Calculation> and <Field> are emitted only when configured;
        // a variable target is preceded by the bare <Text/> marker.
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "SelectAll", HrLabel = "Select", Display = DisplayMode.Augmented },
            new BareCalcChild { PocoProperty = "Calculation", Optional = true, Display = DisplayMode.Native },
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Target", Optional = true, VariableTextMarker = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
