using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;

namespace SharpFM.Model.Scripting;

/// <summary>
/// Self-typed base for shape-driven step POCOs (the curiously recurring
/// pattern, as in <see cref="System.IParsable{TSelf}"/>). Supplies the
/// shape-engine serializers once — <c>TSelf.Metadata</c> resolves through
/// the <see cref="IStepFactory"/> static-abstract constraint. A step
/// overrides <see cref="ScriptStep.PopulateFromXml"/>,
/// <see cref="ScriptStep.PopulateFromDisplay"/>, <see cref="ToDisplayLine"/>,
/// or <see cref="ToXml"/> only where shape-driven behavior isn't sufficient;
/// unrecognized state falls through to these generic defaults.
/// </summary>
public abstract class ScriptStep<TSelf> : ScriptStep
    where TSelf : ScriptStep<TSelf>, IStepFactory
{
    protected ScriptStep(bool enabled) : base(enabled) { }

    public override XElement ToXml() => StepXmlRenderer.Render(this, TSelf.Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, TSelf.Metadata);

    protected internal override void PopulateFromXml(XElement step) =>
        StepXmlParser.Populate(this, step, TSelf.Metadata);

    protected internal override void PopulateFromDisplay(string[] hrParams) =>
        StepDisplayParser.Populate(this, hrParams, TSelf.Metadata);

    /// <summary>Typed counterpart of the <see cref="ScriptStep.FromXml"/> dispatch
    /// for callers that already know the step kind.</summary>
    public static TSelf Parse(XElement step)
    {
        var instance = (TSelf)Activator.CreateInstance(typeof(TSelf), nonPublic: true)!;
        instance.Enabled = step.Attribute("enable")?.Value != "False";
        instance.PopulateFromXml(step);
        return instance;
    }
}
