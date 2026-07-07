using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RelookupFieldContentsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 40;
    public const string XmlName = "Relookup Field Contents";

    public bool WithDialog { get; set; }
    public FieldRef? Target { get; set; }

    /// <summary>
    /// XML-facing inverse of <see cref="WithDialog"/>: canonical
    /// <c>&lt;NoInteract state="…"/&gt;</c> suppresses the dialog, so it is the
    /// negation of the display-facing flag. The shape binds this so the raw
    /// state round-trips without re-applying the inversion.
    /// </summary>
    public bool NoInteract { get => !WithDialog; set => WithDialog = !value; }

    private RelookupFieldContentsStep() : base(false) { }

    public RelookupFieldContentsStep(
        bool withDialog = true,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Relookup Field Contents [ " + "With dialog: " + (WithDialog ? "On" : "Off") + " ; " + (Target?.ToDisplayString() ?? "") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<RelookupFieldContentsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool withDialog_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        FieldRef? target = null;
        foreach (var tok in tokens) { if (!tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(tok)) { target = FieldRef.FromDisplayToken(tok); break; } }
        return new RelookupFieldContentsStep(withDialog_v, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/relookup-field-contents.html",
        // Canonical 040-RelookupFieldContents: NoInteract (always present, the
        // inverse of WithDialog) then a <Field> omitted until a target is bound
        // (Optional).
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteract", HrLabel = "With dialog" },
            new FieldChild("Field") { PocoProperty = "Target", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
