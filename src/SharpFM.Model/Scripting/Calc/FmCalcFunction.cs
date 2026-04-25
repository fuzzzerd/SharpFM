using System.Collections.Generic;

namespace SharpFM.Model.Scripting.Calc;

/// <summary>
/// One built-in FileMaker calculation function. <see cref="Signature"/> is a
/// human-readable form (e.g. <c>Length(text)</c>) shown in completion
/// tooltips; <see cref="Description"/> is a one-line summary.
///
/// <para><see cref="Params"/> describes each positional parameter. When a
/// parameter has a <see cref="FmCalcFunctionParam.ValidValues"/> list, the
/// completion provider offers those keywords when the caret is inside that
/// argument position. Functions whose params are open-ended (numbers,
/// fields, expressions) leave <see cref="Params"/> empty — the catalog only
/// models keyword arguments.</para>
/// </summary>
public sealed record FmCalcFunction(
    string Name,
    FunctionCategory Category,
    string Signature,
    string Description,
    IReadOnlyList<FmCalcFunctionParam> Params);
