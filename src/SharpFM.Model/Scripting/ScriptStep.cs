using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting;

/// <summary>
/// Abstract base of the typed script-step domain model. Concrete
/// subclasses are typed POCOs in <c>SharpFM.Model.Scripting.Steps</c>
/// that carry step-specific fields with full type safety and own their
/// own <see cref="ToXml"/> / <see cref="ToDisplayLine"/> logic.
///
/// <para>
/// The domain model is the sole source of truth for serialization: if
/// a step round-trips losslessly it does so because its typed POCO
/// carries every relevant field as typed state. The one exception is
/// <c>RawStep</c>, the forward-compat fallback that wraps unknown
/// step elements verbatim.
/// </para>
/// </summary>
public abstract class ScriptStep
{
    public bool Enabled { get; set; }

    protected ScriptStep(bool enabled)
    {
        Enabled = enabled;
    }

    public abstract XElement ToXml();
    public abstract string ToDisplayLine();

    public virtual List<ScriptDiagnostic> Validate(int lineIndex) => new();

    /// <summary>
    /// True when this step round-trips losslessly through the display-text
    /// editor. Typed POCOs return <c>true</c>; <c>RawStep</c> overrides
    /// to false because unknown elements have no shape contract to
    /// parse display-text edits against.
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
