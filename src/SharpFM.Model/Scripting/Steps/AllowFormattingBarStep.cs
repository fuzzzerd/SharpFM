using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for AllowFormattingBarStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Set state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class AllowFormattingBarStep : ScriptStep<AllowFormattingBarStep>, IStepFactory
{
    public const int XmlId = 115;
    public const string XmlName = "Allow Formatting Bar";

    /// <summary>The boolean setting encoded as <c>&lt;Set state="True|False"/&gt;</c>.</summary>
    public bool Set { get; set; }

    private AllowFormattingBarStep() : base(false) { }

    public AllowFormattingBarStep(bool set = false, bool enabled = true)
        : base(enabled)
    {
        Set = set;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/allow-formatting-bar.html",
        Shape = [new BoolStateChild("Set")],
    };
}
