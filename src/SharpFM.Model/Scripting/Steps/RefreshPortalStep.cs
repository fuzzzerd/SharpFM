using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RefreshPortalStep : ScriptStep<RefreshPortalStep>, IStepFactory
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

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/refresh-portal.html",
        // Canonical unconfigured form is empty: ObjectName is omitted when blank.
        Shape =
        [
            new NamedCalcChild("ObjectName") { PocoProperty = "ObjectName", HrLabel = "Object Name", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
