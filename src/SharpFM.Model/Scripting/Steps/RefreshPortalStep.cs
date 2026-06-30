using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RefreshPortalStep : ScriptStep, IStepFactory
{
    public const int XmlId = 180;
    public const string XmlName = "Refresh Portal";

    public Calculation? ObjectName { get; set; }

    private RefreshPortalStep() : base(false) { }

    public RefreshPortalStep(
        Calculation? objectName = null,
        bool enabled = true)
        : base(enabled)
    {
        ObjectName = objectName;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Refresh Portal [ " + "Object Name: " + (ObjectName?.Text ?? "") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<RefreshPortalStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? objectName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Object Name:", StringComparison.OrdinalIgnoreCase)) { objectName_v = new Calculation(tok.Substring(12).Trim()); break; } }
        return new RefreshPortalStep(objectName_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/refresh-portal.html",
        // Canonical unconfigured form is empty: ObjectName is omitted when blank.
        Shape =
        [
            new NamedCalcChild("ObjectName") { PocoProperty = "ObjectName", HrLabel = "Object Name", Optional = true },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Object Name",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
