using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class RevertTransactionStep : ScriptStep<RevertTransactionStep>, IStepFactory
{
    public const int XmlId = 207;
    public const string XmlName = "Revert Transaction";

    public bool Option { get; set; }
    public Calculation? Condition { get; set; }
    public Calculation? ErrorCode { get; set; }
    public Calculation? ErrorMessage { get; set; }

    private RevertTransactionStep() : base(false) { }

    public RevertTransactionStep(
        bool option = false,
        Calculation? condition = null,
        Calculation? errorCode = null,
        Calculation? errorMessage = null,
        bool enabled = true)
        : base(enabled)
    {
        Option = option;
        Condition = condition;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/revert-transaction.html",
        // Canonical: Option, then the optional Condition / ErrorCode /
        // ErrorMessage calculations (omitted when unconfigured).
        Shape =
        [
            new BoolStateChild("Option") { PocoProperty = "Option", Display = DisplayMode.Native },
            new NamedCalcChild("Condition") { PocoProperty = "Condition", HrLabel = "Condition", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
            new NamedCalcChild("ErrorCode") { PocoProperty = "ErrorCode", HrLabel = "Error Code", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
            new NamedCalcChild("ErrorMessage") { PocoProperty = "ErrorMessage", HrLabel = "Error Message", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
        ],
    };
}
