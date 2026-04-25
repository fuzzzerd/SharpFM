using System.Collections.Generic;

namespace SharpFM.Model.Scripting.Calc;

/// <summary>
/// Parses the param list out of a function signature string like
/// <c>JSONGetElement(json; keyOrIndexOrPath)</c> so completion items can
/// tab through placeholders without us hand-authoring a per-function param
/// list. Variadic markers like <c>{; field...}</c> are dropped — anything
/// before the first <c>{</c> is taken as the named portion.
/// </summary>
public static class FmCalcSignatureParser
{
    public static IReadOnlyList<FmCalcFunctionParam> ParseParams(string signature)
    {
        if (string.IsNullOrWhiteSpace(signature)) return System.Array.Empty<FmCalcFunctionParam>();

        var openParen = signature.IndexOf('(');
        var closeParen = signature.LastIndexOf(')');
        if (openParen < 0 || closeParen < openParen) return System.Array.Empty<FmCalcFunctionParam>();

        var inner = signature.Substring(openParen + 1, closeParen - openParen - 1);

        // Drop any variadic / optional region. Catalog signatures use
        // {; ...} for "and more like this" — we don't model that as a
        // separate stop, so trim it off.
        var brace = inner.IndexOf('{');
        if (brace >= 0) inner = inner.Substring(0, brace);

        if (string.IsNullOrWhiteSpace(inner)) return System.Array.Empty<FmCalcFunctionParam>();

        var result = new List<FmCalcFunctionParam>();
        foreach (var part in inner.Split(';'))
        {
            var name = part.Trim().TrimStart('[').TrimEnd(']').Trim();
            if (name.Length == 0) continue;
            result.Add(new FmCalcFunctionParam(name));
        }
        return result;
    }
}
