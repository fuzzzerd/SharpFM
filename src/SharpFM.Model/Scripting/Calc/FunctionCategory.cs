namespace SharpFM.Model.Scripting.Calc;

/// <summary>
/// FileMaker calculation function categories. Mirrors the grouping in
/// FileMaker's calculation dialog so completion menus and TextMate scopes
/// (<c>support.function.&lt;category&gt;.fmcalc</c>) line up.
/// </summary>
public enum FunctionCategory
{
    Text,
    TextFormatting,
    Number,
    Date,
    Time,
    Aggregate,
    Summary,
    Financial,
    Trigonometric,
    Logical,
    Get,
    Container,
    Json,
    Sql,
    External,
    Design,
}
