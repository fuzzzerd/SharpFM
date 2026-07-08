using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting;

/// <summary>
/// Self-typed base for shape-driven step POCOs (the curiously recurring
/// pattern, as in <see cref="System.IParsable{TSelf}"/>). Supplies the
/// shape-engine serializers once — <c>TSelf.Metadata</c> resolves through
/// the <see cref="IStepFactory"/> static-abstract constraint — and a typed
/// <see cref="Parse"/> factory returning the concrete step type. Steps
/// with hand-written XML parsing derive plain <see cref="ScriptStep"/>
/// instead and declare everything explicitly.
/// </summary>
public abstract class ScriptStep<TSelf> : ScriptStep
    where TSelf : ScriptStep<TSelf>, IStepFactory
{
    protected ScriptStep(bool enabled) : base(enabled) { }

    public override XElement ToXml() => StepXmlRenderer.Render(this, TSelf.Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, TSelf.Metadata);

    /// <summary>Typed counterpart of the <see cref="ScriptStep.FromXml"/> dispatch
    /// for callers that already know the step kind.</summary>
    public static TSelf Parse(XElement step) => StepXmlParser.Parse<TSelf>(step, TSelf.Metadata);
}
