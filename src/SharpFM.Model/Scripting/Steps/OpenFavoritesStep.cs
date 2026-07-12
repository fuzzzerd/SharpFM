using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for OpenFavoritesStep: the step's only XML state is the three
/// &lt;Step&gt; attributes (enable/id/name). All round-tripped exactly.
/// No child elements in FM Pro's clipboard output; no hidden state.
/// </summary>
public sealed class OpenFavoritesStep : ScriptStep<OpenFavoritesStep>, IStepFactory
{
    public const int XmlId = 183;
    public const string XmlName = "Open Favorites";

    private OpenFavoritesStep() : base(false) { }

    public OpenFavoritesStep(bool enabled = true) : base(enabled) { }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "open menu item",
    };
}
