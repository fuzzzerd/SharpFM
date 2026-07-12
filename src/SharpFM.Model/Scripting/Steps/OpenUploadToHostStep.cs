using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for OpenUploadToHostStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class OpenUploadToHostStep : ScriptStep<OpenUploadToHostStep>, IStepFactory
{
    public const int XmlId = 172;
    public const string XmlName = "Open Upload to Host";

    private OpenUploadToHostStep() : base(false) { }

    public OpenUploadToHostStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "open menu item",
        HelpUrl = "https://help.claris.com/en/pro-help/content/upload-to-server.html",
    };
}
