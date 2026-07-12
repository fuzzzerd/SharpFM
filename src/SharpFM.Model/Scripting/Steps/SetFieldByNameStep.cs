using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetFieldByNameStep : ScriptStep<SetFieldByNameStep>, IStepFactory
{
    public const int XmlId = 147;
    public const string XmlName = "Set Field By Name";

    public Calculation TargetFieldName { get; set; } = new("");
    public Calculation CalculatedResult { get; set; } = new("");

    private SetFieldByNameStep() : base(false) { }

    public SetFieldByNameStep(
        Calculation? targetFieldName = null,
        Calculation? calculatedResult = null,
        bool enabled = true)
        : base(enabled)
    {
        TargetFieldName = targetFieldName ?? new Calculation("");
        CalculatedResult = calculatedResult ?? new Calculation("");
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-field-by-name.html",
        // TargetName and Result wrappers are omitted by the unconfigured form (Optional).
        Shape =
        [
            new NamedCalcChild("TargetName") { PocoProperty = "TargetFieldName", HrLabel = "Target field name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new NamedCalcChild("Result") { PocoProperty = "CalculatedResult", HrLabel = "Calculated result", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
    };
}
