using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for AllowUserAbortStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Set state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class AllowUserAbortStep : ScriptStep, IStepFactory
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<AllowUserAbortStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<AllowUserAbortStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        Shape = [new BoolStateChild("Set")],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
