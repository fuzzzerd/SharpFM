namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// Discriminated union for Perform Script's two script-selection modes.
/// <list type="bullet">
///   <item><c>ByReference</c> — static script reference carrying id+name; emits a <c>&lt;Script&gt;</c> element.</item>
///   <item><c>ByCalculation</c> — dynamic script name resolved at runtime; emits a <c>&lt;Calculated&gt;&lt;Calculation/&gt;&lt;/Calculated&gt;</c> wrapper.</item>
/// </list>
/// The XML discriminant is the presence of <c>&lt;Script&gt;</c> vs. <c>&lt;Calculated&gt;</c>; they are mutually exclusive in FM Pro output.
/// </summary>
public abstract record PerformScriptTarget
{
    private PerformScriptTarget() { }

    public sealed record ByReference(NamedRef Script) : PerformScriptTarget;

    public sealed record ByCalculation(Calculation NameCalc) : PerformScriptTarget;
}
