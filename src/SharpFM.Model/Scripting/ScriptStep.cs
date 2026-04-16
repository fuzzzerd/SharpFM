using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting;

/// <summary>
/// Abstract base of the typed script-step domain model. Concrete
/// subclasses fall into two buckets:
///
/// <list type="bullet">
/// <item>
/// <b>Typed POCOs</b> (e.g. <c>GoToLayoutStep</c> in
/// <c>SharpFM.Model.Scripting.Steps</c>) — carry step-specific fields
/// with full type safety and own their own <see cref="ToXml"/> /
/// <see cref="ToDisplayLine"/> logic. Serialization is mechanical and
/// lossless because every byte of source XML that matters lives as
/// typed state on the POCO.
/// </item>
/// <item>
/// <b><c>RawStep</c></b> — a transitional catch-all that wraps the source
/// <see cref="XElement"/> verbatim and delegates rendering / validation
/// to the stateless catalog-driven helpers. Used for every step that
/// does not yet have a typed POCO. Serialization is lossless for the
/// raw element but display-text editing of unmigrated steps relies on
/// the generic catalog path and may be subtly different from
/// FileMaker Pro's canonical formatting until the corresponding typed
/// POCO lands.
/// </item>
/// </list>
///
/// <para>
/// Handlers, the old <c>StepParamValue</c> bag, and the
/// <c>SpecializedDisplayRenderer</c> hook are all gone. The domain
/// model is now the sole source of truth for serialization: if a step
/// round-trips losslessly it does so because its typed POCO carries
/// every relevant field, not because a side-channel preserved extras.
/// </para>
/// </summary>
public abstract class ScriptStep
{
    public StepDefinition? Definition { get; }
    public bool Enabled { get; set; }

    protected ScriptStep(StepDefinition? definition, bool enabled)
    {
        Definition = definition;
        Enabled = enabled;
    }

    public abstract XElement ToXml();
    public abstract string ToDisplayLine();

    public virtual List<ScriptDiagnostic> Validate(int lineIndex) => new();

    /// <summary>
    /// True when this step round-trips losslessly through the display-text
    /// editor. Typed POCOs return <c>true</c> because they carry every
    /// relevant field as typed state. <see cref="RawStep"/> overrides and
    /// defers to <c>RawStepAllowList</c> — a catalog-path step is sealed
    /// (read-only in the display editor) unless explicitly verified on
    /// the allow-list. Sealed steps remain fully lossless at the XML
    /// level; they just can't be edited as display text.
    /// </summary>
    public virtual bool IsFullyEditable => true;

    /// <summary>
    /// Entry point for parsing a script step XML element into the
    /// domain model. Dispatches through <see cref="StepXmlFactory"/> to
    /// either a registered typed POCO or a <c>RawStep</c> fallback.
    /// </summary>
    public static ScriptStep FromXml(XElement stepElement) =>
        StepXmlFactory.Create(stepElement);
}
