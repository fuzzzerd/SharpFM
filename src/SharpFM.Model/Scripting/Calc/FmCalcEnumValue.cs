namespace SharpFM.Model.Scripting.Calc;

/// <summary>
/// One valid value for a function parameter that takes an enumerated keyword.
/// <see cref="Description"/> is optional and surfaces in completion tooltips
/// when present (e.g. each <c>Get(...)</c> selector has its own one-liner).
/// </summary>
public sealed record FmCalcEnumValue(string Name, string? Description = null);
