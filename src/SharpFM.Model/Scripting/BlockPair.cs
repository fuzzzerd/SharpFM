namespace SharpFM.Model.Scripting;

/// <summary>
/// Role of a step in a block-pair construct (if/else/end-if, loop/end-loop).
/// </summary>
public enum BlockPairRole
{
    Open,
    Middle,
    Close,
}

/// <summary>
/// Block-pair metadata carried by step POCOs that open, continue, or
/// close a block. Drives indentation in <see cref="FmScript.ToDisplayLines"/>
/// and matching-partner validation in <see cref="ScriptValidator"/>.
/// </summary>
public sealed record StepBlockPair
{
    public required BlockPairRole Role { get; init; }

    /// <summary>
    /// Canonical names of the other steps that participate in this
    /// block. E.g. an If's partners are ["Else", "Else If", "End If"].
    /// </summary>
    public required string[] Partners { get; init; }
}
