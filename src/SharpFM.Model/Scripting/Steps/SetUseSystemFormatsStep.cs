using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetUseSystemFormatsStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Set state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class SetUseSystemFormatsStep : ScriptStep<SetUseSystemFormatsStep>, IStepFactory
{
    public const int XmlId = 94;
    public const string XmlName = "Set Use System Formats";

    /// <summary>The <c>Use system formats</c> flag on the step.</summary>
    public bool UseSystemFormats { get; set; }

    private SetUseSystemFormatsStep() : base(false) { }

    public SetUseSystemFormatsStep(bool usesystemformats = true, bool enabled = true)
        : base(enabled)
    {
        UseSystemFormats = usesystemformats;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-use-system-formats.html",
        Shape =
        [
            new BoolStateChild("Set") { PocoProperty = "UseSystemFormats", HrLabel = "Use system formats" },
        ],
    };
}
