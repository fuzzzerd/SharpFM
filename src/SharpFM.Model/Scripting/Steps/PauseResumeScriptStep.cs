using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PauseResumeScriptStep : ScriptStep<PauseResumeScriptStep>, IStepFactory
{
    public const int XmlId = 62;
    public const string XmlName = "Pause/Resume Script";

    public string PauseTime { get; set; }
    public Calculation? DurationSeconds { get; set; }

    private PauseResumeScriptStep() : base(false) { PauseTime = "ForDuration"; }

    public PauseResumeScriptStep(
        string pauseTime = "ForDuration",
        Calculation? durationSeconds = null,
        bool enabled = true)
        : base(enabled)
    {
        PauseTime = pauseTime;
        DurationSeconds = durationSeconds;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/pause-resume-script.html",
        // Canonical 062-PauseResumeScript: only <PauseTime>; the bare
        // <Calculation> duration is omitted when blank (Optional).
        Shape =
        [
            new EnumValueChild("PauseTime") { PocoProperty = "PauseTime", DefaultValue = "ForDuration", ValidValues = ["Indefinitely", "ForDuration"], DisplayValues = ["Indefinitely", "Duration (seconds)"] },
            new BareCalcChild { PocoProperty = "DurationSeconds", HrLabel = "Duration (seconds)", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
