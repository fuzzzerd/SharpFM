using System.Collections.Generic;

namespace SharpFM.Model.Scripting.Calc;

/// <summary>
/// One parameter of a built-in calculation function. <see cref="ValidValues"/>
/// is the keyword set the parameter accepts (e.g. <c>Get(parameter)</c>'s
/// selectors, <c>JSONSetElement</c>'s <c>type</c> values); <c>null</c> when
/// the parameter is open-ended (a number, string, field, expression, …).
/// </summary>
public sealed record FmCalcFunctionParam(
    string Name,
    string? Description = null,
    IReadOnlyList<FmCalcEnumValue>? ValidValues = null);
