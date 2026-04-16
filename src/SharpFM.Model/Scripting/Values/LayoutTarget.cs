namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// Discriminated-union-style hierarchy for the target of a "Go to Layout"
/// step. FileMaker's script XML encodes this as a <c>&lt;LayoutDestination
/// value="..."/&gt;</c> element plus an optional <c>&lt;Layout&gt;</c> child
/// whose shape depends on the destination kind:
///
/// <list type="bullet">
///   <item><c>OriginalLayout</c> — no Layout element at all.</item>
///   <item><c>SelectedLayout</c> — Layout element carries id+name attributes.</item>
///   <item><c>LayoutNameByCalc</c> — Layout element contains a nested Calculation.</item>
///   <item><c>LayoutNumberByCalc</c> — same shape as ByNameCalc, different semantics.</item>
/// </list>
///
/// The closed record hierarchy makes pattern matching exhaustive at the
/// call site and prevents invalid combinations (e.g. a named ref with a
/// calculation expression) from being expressible.
/// </summary>
public abstract record LayoutTarget
{
    private LayoutTarget() { }

    /// <summary>
    /// FileMaker's wire value for the <c>&lt;LayoutDestination&gt;</c> attribute.
    /// These are the actual strings FM Pro writes — note that FM uses
    /// <c>LayoutNameByCalc</c> (not <c>LayoutNameByCalculation</c>).
    /// </summary>
    public abstract string WireValue { get; }

    public sealed record Original : LayoutTarget
    {
        public override string WireValue => "OriginalLayout";
    }

    public sealed record Named(NamedRef Layout) : LayoutTarget
    {
        public override string WireValue => "SelectedLayout";
    }

    public sealed record ByNameCalc(Calculation Calc) : LayoutTarget
    {
        public override string WireValue => "LayoutNameByCalc";
    }

    public sealed record ByNumberCalc(Calculation Calc) : LayoutTarget
    {
        public override string WireValue => "LayoutNumberByCalc";
    }
}
