using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for AllowUserAbortStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Set state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class AllowUserAbortStep : ScriptStep<AllowUserAbortStep>, IStepFactory
{
    public const int XmlId = 85;
    public const string XmlName = "Allow User Abort";

    /// <summary>The boolean setting encoded as <c>&lt;Set state="True|False"/&gt;</c>.</summary>
    public bool Set { get; set; }

    private AllowUserAbortStep() : base(false) { }

    public AllowUserAbortStep(bool set = false, bool enabled = true)
        : base(enabled)
    {
        Set = set;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        Shape = [new BoolStateChild("Set")],
    };
}
