namespace SharpFM.Model.Scripting.Calc;

/// <summary>
/// One built-in FileMaker calculation function. <see cref="Signature"/> is
/// a human-readable form (e.g. <c>Length(text)</c>) shown in completion
/// tooltips; <see cref="Description"/> is a one-line summary.
/// </summary>
public sealed record FmCalcFunction(
    string Name,
    FunctionCategory Category,
    string Signature,
    string Description);
