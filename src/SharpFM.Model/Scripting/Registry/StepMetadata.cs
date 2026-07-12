using System.Collections.Generic;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Registry;

/// <summary>
/// Typed self-description of a script-step POCO. Every POCO that
/// implements <see cref="IStepFactory"/> exposes one of these as its
/// static <c>Metadata</c> property, and <see cref="StepRegistry"/>
/// discovers them via reflection at first access.
///
/// <para>
/// Construction is not described here: <see cref="StepRegistry"/> always
/// constructs a blank instance and calls its (possibly overridden)
/// <see cref="ScriptStep.PopulateFromXml"/> / <see cref="ScriptStep.PopulateFromDisplay"/>.
/// A step opts into hand-written parsing by overriding those methods, not
/// by describing it on this record.
/// </para>
/// </summary>
public sealed record StepMetadata
{
    /// <summary>Canonical FileMaker step name, e.g. "Set Error Capture".</summary>
    public required string Name { get; init; }

    /// <summary>Numeric step id FileMaker emits in &lt;Step id="..."/&gt;.</summary>
    public required int Id { get; init; }

    /// <summary>Category grouping used by completion UIs (e.g. "control").</summary>
    public required string Category { get; init; }

    /// <summary>Claris help URL for this step.</summary>
    public string? HelpUrl { get; init; }

    /// <summary>
    /// One-line display summary shown alongside the step name in
    /// completion — e.g. <c>"[ condition ]"</c> for <c>If</c>.
    /// </summary>
    public string? HrSignature { get; init; }

    /// <summary>
    /// Block-pair information for control-flow steps (If/End If, Loop/End
    /// Loop, etc.). Null for standalone steps.
    /// </summary>
    public StepBlockPair? BlockPair { get; init; }

    /// <summary>
    /// Ordered declarative description of the step's wire shape — the single
    /// source of truth that drives XML emission, parsing, validation, and the
    /// display consumers (param synthesis, script validation, completion via
    /// <see cref="Shapes.ShapeHrView"/>). Element order in emitted XML matches
    /// this list, satisfying FileMaker's canonical element-order requirements.
    /// </summary>
    public IReadOnlyList<ShapeNode> Shape { get; init; } = [];

    /// <summary>Behavioural intelligence — tooltip / lint source.</summary>
    public StepNotes? Notes { get; init; }
}
