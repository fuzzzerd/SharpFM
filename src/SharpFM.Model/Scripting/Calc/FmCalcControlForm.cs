namespace SharpFM.Model.Scripting.Calc;

/// <summary>
/// One of the FileMaker calculation control forms (<c>Let</c>, <c>Case</c>,
/// etc.). <see cref="Snippet"/> uses Monaco-style <c>${N:placeholder}</c>
/// tab-stops so completion accept inserts the full form with the first slot
/// pre-selected.
/// </summary>
public sealed record FmCalcControlForm(
    string Name,
    string Signature,
    string Description,
    string Snippet);
