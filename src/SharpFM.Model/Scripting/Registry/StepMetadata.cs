using System;
using System.Collections.Generic;
using System.Xml.Linq;

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
/// bridge them into the legacy <c>StepXmlFactory</c> and
/// <c>StepDisplayFactory</c> surfaces without touching the POCO's
/// declaration site. Once the legacy surfaces are retired the delegates
/// become the sole construction path.
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

    /// <summary>Typed descriptions of the step's parameters.</summary>
    public IReadOnlyList<ParamMetadata> Params { get; init; } = [];

    /// <summary>Behavioural intelligence — tooltip / lint source.</summary>
    public StepNotes? Notes { get; init; }

    /// <summary>
    /// Delegate that constructs a POCO instance from a source
    /// <c>&lt;Step&gt;</c> element. Usually assigned via method-group
    /// reference to the POCO's static <c>FromXml</c> method.
    /// </summary>
    public Func<XElement, ScriptStep>? FromXml { get; init; }

    /// <summary>
    /// Delegate that constructs a POCO instance from parsed display-text
    /// tokens. Usually assigned via method-group reference to the POCO's
    /// static <c>FromDisplayParams</c> method.
    /// </summary>
    public Func<bool, string[], ScriptStep>? FromDisplay { get; init; }
}
