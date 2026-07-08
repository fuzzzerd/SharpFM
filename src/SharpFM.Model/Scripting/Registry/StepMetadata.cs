using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Registry;

/// <summary>
/// Typed self-description of a script-step POCO. Every POCO that
/// implements <see cref="IStepFactory"/> exposes one of these as its
/// static <c>Metadata</c> property, and <see cref="StepRegistry"/>
/// discovers them via reflection at first access.
///
/// <para>
/// The factory delegates (<see cref="FromXml"/>,
/// <see cref="FromDisplay"/>) live on the metadata so the registry can
/// bridge them into the <c>StepXmlFactory</c> and
/// <c>StepDisplayFactory</c> surfaces without touching the POCO's
/// declaration site. Leaving a delegate <c>null</c> tells the registry to
/// synthesize shape-driven parsing from <see cref="Shape"/> instead; a
/// step assigns one explicitly only to opt out with a hand-written parser.
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

    /// <summary>
    /// Delegate that constructs a POCO instance from a source
    /// <c>&lt;Step&gt;</c> element. <c>null</c> (the default) means
    /// <see cref="StepRegistry"/> synthesizes shape-driven parsing from
    /// <see cref="Shape"/>; assign explicitly only for a hand-written parser.
    /// </summary>
    public Func<XElement, ScriptStep>? FromXml { get; init; }

    /// <summary>
    /// Delegate that constructs a POCO instance from parsed display-text
    /// tokens. <c>null</c> (the default) means <see cref="StepRegistry"/>
    /// synthesizes shape-driven parsing from <see cref="Shape"/>; assign
    /// explicitly only for a hand-written parser.
    /// </summary>
    public Func<bool, string[], ScriptStep>? FromDisplay { get; init; }
}
