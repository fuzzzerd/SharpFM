using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RefreshObjectStep : ScriptStep<RefreshObjectStep>, IStepFactory
{
    public const int XmlId = 167;
    public const string XmlName = "Refresh Object";

    public Calculation ObjectName { get; set; } = new("");
    public Calculation Repetition { get; set; } = new("");

    private RefreshObjectStep() : base(false) { }

    public RefreshObjectStep(
        Calculation? objectName = null,
        Calculation? repetition = null,
        bool enabled = true)
        : base(enabled)
    {
        ObjectName = objectName ?? new Calculation("");
        Repetition = repetition ?? new Calculation("");
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/refresh-object.html",
        // ObjectName and Repetition wrappers are omitted by the unconfigured
        // form (Optional).
        Shape =
        [
            new NamedCalcChild("ObjectName") { PocoProperty = "ObjectName", HrLabel = "Object Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new NamedCalcChild("Repetition") { PocoProperty = "Repetition", HrLabel = "Repetition", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
    };
}
