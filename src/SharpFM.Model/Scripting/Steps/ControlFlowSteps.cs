using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed POCOs for FileMaker's block-pair control-flow steps:
/// <c>If</c>, <c>Else If</c>, <c>Else</c>, <c>End If</c>.
/// <para>
/// <c>If</c> and <c>Else If</c> carry a single <see cref="Calculation"/>
/// condition. The catalog defines a <c>Restore</c> param for these two,
/// but FM Pro never emits it in clipboard output and the old handler
/// explicitly ignored it — the typed POCO follows suit.
/// </para>
/// <para>
/// <c>Else</c> and <c>End If</c> have no fields and no children; the
/// typed POCOs still need to exist so they bypass the generic catalog
/// display-render path, which previously corrupted <c>If</c>'s rendering
/// in certain calculation shapes (e.g. the old path emitted
/// <c>If [ Off ]</c> for <c>If [ Get ( FoundCount ) > 0 ]</c>). Block-pair
/// indentation is driven by <c>StepDefinition.BlockPair</c> from the
/// catalog and is handled by <see cref="FmScript.ToDisplayLines"/>, not
/// by the individual POCOs.
/// </para>
/// </summary>
/// <summary>
/// Zero-loss audit for <see cref="IfStep"/>:
/// <list type="bullet">
/// <item><c>Step</c> attributes (<c>enable</c>, <c>id</c>, <c>name</c>) — round-tripped.</item>
/// <item><c>&lt;Calculation&gt;</c> CDATA body — round-tripped via <see cref="Calculation"/>.</item>
/// <item><c>&lt;Restore state="..."/&gt;</c> — <b>intentionally dropped</b>. Upstream agentic-fm
/// snippets include it, but FM Pro never changes the value and never emits the element in
/// clipboard output; it carries no information worth round-tripping. See
/// <c>docs/advanced-filemaker-scripting-syntax.md</c> for the "what to drop vs. surface"
/// guidance this follows.</item>
/// </list>
/// </summary>
public sealed class IfStep : ScriptStep, IStepFactory
{
    public Calculation Condition { get; set; }

    public IfStep(bool enabled, Calculation condition)
        : base(BuildLegacyDefinition(), enabled)
    {
        Condition = condition;
    }

    // Transitional: legacy consumers (FmScript.ToDisplayLines, etc.) still
    // read step.Definition.BlockPair for indent decisions. Project the
    // pieces Metadata carries into a synthesized StepDefinition until those
    // consumers migrate to StepRegistry in a later phase.
    private static StepDefinition BuildLegacyDefinition() => new()
    {
        Name = "If",
        Id = 68,
        BlockPair = new StepBlockPair
        {
            Role = BlockPairRole.Open,
            Partners = ["Else", "Else If", "End If"],
        },
    };

    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Register sibling control-flow steps that haven't migrated to IStepFactory yet.")]
    [ModuleInitializer]
    internal static void Register()
    {
        // IfStep itself registers via StepRegistry's reflection scan (IStepFactory).
        // The siblings below migrate in the sweep phase; for now their factory
        // registrations stay here so the legacy StepXmlFactory / StepDisplayFactory
        // surfaces continue to return typed POCOs.

        StepXmlFactory.Register("Else If", ElseIfStep.FromXml);
        StepDisplayFactory.Register("Else If", ElseIfStep.FromDisplayParams);

        StepXmlFactory.Register("Else", ElseStep.FromXml);
        StepDisplayFactory.Register("Else", ElseStep.FromDisplayParams);

        StepXmlFactory.Register("End If", EndIfStep.FromXml);
        StepDisplayFactory.Register("End If", EndIfStep.FromDisplayParams);

        StepXmlFactory.Register("Loop", LoopStep.FromXml);
        StepDisplayFactory.Register("Loop", LoopStep.FromDisplayParams);

        StepXmlFactory.Register("End Loop", EndLoopStep.FromXml);
        StepDisplayFactory.Register("End Loop", EndLoopStep.FromDisplayParams);

        StepXmlFactory.Register("Exit Loop If", ExitLoopIfStep.FromXml);
        StepDisplayFactory.Register("Exit Loop If", ExitLoopIfStep.FromDisplayParams);
    }

    public static new ScriptStep FromXml(XElement step) =>
        new IfStep(
            step.Attribute("enable")?.Value != "False",
            ReadCalculation(step));

    public override XElement ToXml() =>
        BuildConditionedStep(this, "If", 68, Condition);

    public override string ToDisplayLine() => $"If [ {Condition.Text} ]";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new IfStep(enabled, ParseCondition(hrParams));

    public static StepMetadata Metadata { get; } = new()
    {
        Name = "If",
        Id = 68,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/if-script-step.html",
        HrSignature = "[ condition ]",
        BlockPair = new StepBlockPair
        {
            Role = BlockPairRole.Open,
            Partners = ["Else", "Else If", "End If"],
        },
        Params =
        [
            new ParamMetadata
            {
                Name = "condition",
                XmlElement = "Calculation",
                Type = "calculation",
                // Intentionally no HrLabel — FM Pro's display renders the
                // calc without a label prefix (e.g. "If [ $x > 0 ]"). The
                // completion snippet synthesizer uses Name as the
                // placeholder hint so it reads "If [ ${1:condition} ]".
                Required = true,
            },
        ],
        Notes = new StepNotes
        {
            Constraints = "Requires a matching End If step.",
        },
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };

    internal static Calculation ReadCalculation(XElement step)
    {
        var calc = step.Element("Calculation");
        return calc is not null ? Calculation.FromXml(calc) : new Calculation("");
    }

    internal static Calculation ParseCondition(string[] hrParams) =>
        hrParams.Length >= 1 ? new Calculation(hrParams[0].Trim()) : new Calculation("");

    internal static XElement BuildConditionedStep(ScriptStep owner, string name, int id, Calculation condition)
    {
        var step = new XElement("Step",
            new XAttribute("enable", owner.Enabled ? "True" : "False"),
            new XAttribute("id", id),
            new XAttribute("name", name));
        step.Add(condition.ToXml());
        return step;
    }
}

public sealed class ElseIfStep : ScriptStep
{
    public Calculation Condition { get; set; }

    public ElseIfStep(bool enabled, Calculation condition)
        : base(StepCatalogLoader.ByName["Else If"], enabled)
    {
        Condition = condition;
    }

    public static new ScriptStep FromXml(XElement step) =>
        new ElseIfStep(
            step.Attribute("enable")?.Value != "False",
            IfStep.ReadCalculation(step));

    public override XElement ToXml() =>
        IfStep.BuildConditionedStep(this, "Else If", 125, Condition);

    public override string ToDisplayLine() => $"Else If [ {Condition.Text} ]";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ElseIfStep(enabled, IfStep.ParseCondition(hrParams));
}

public sealed class ElseStep : ScriptStep
{
    public ElseStep(bool enabled)
        : base(StepCatalogLoader.ByName["Else"], enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        new ElseStep(step.Attribute("enable")?.Value != "False");

    public override XElement ToXml() => BuildBareStep(this, "Else", 69);

    public override string ToDisplayLine() => "Else";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ElseStep(enabled);

    internal static XElement BuildBareStep(ScriptStep owner, string name, int id) =>
        new("Step",
            new XAttribute("enable", owner.Enabled ? "True" : "False"),
            new XAttribute("id", id),
            new XAttribute("name", name));
}

public sealed class EndIfStep : ScriptStep
{
    public EndIfStep(bool enabled)
        : base(StepCatalogLoader.ByName["End If"], enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        new EndIfStep(step.Attribute("enable")?.Value != "False");

    public override XElement ToXml() => ElseStep.BuildBareStep(this, "End If", 70);

    public override string ToDisplayLine() => "End If";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new EndIfStep(enabled);
}

public sealed class LoopStep : ScriptStep
{
    public LoopStep(bool enabled)
        : base(StepCatalogLoader.ByName["Loop"], enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        new LoopStep(step.Attribute("enable")?.Value != "False");

    public override XElement ToXml() => ElseStep.BuildBareStep(this, "Loop", 71);

    public override string ToDisplayLine() => "Loop";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new LoopStep(enabled);
}

public sealed class EndLoopStep : ScriptStep
{
    public EndLoopStep(bool enabled)
        : base(StepCatalogLoader.ByName["End Loop"], enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        new EndLoopStep(step.Attribute("enable")?.Value != "False");

    public override XElement ToXml() => ElseStep.BuildBareStep(this, "End Loop", 73);

    public override string ToDisplayLine() => "End Loop";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new EndLoopStep(enabled);
}

public sealed class ExitLoopIfStep : ScriptStep
{
    public Calculation Condition { get; set; }

    public ExitLoopIfStep(bool enabled, Calculation condition)
        : base(StepCatalogLoader.ByName["Exit Loop If"], enabled)
    {
        Condition = condition;
    }

    public static new ScriptStep FromXml(XElement step) =>
        new ExitLoopIfStep(
            step.Attribute("enable")?.Value != "False",
            IfStep.ReadCalculation(step));

    public override XElement ToXml() =>
        IfStep.BuildConditionedStep(this, "Exit Loop If", 72, Condition);

    public override string ToDisplayLine() => $"Exit Loop If [ {Condition.Text} ]";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ExitLoopIfStep(enabled, IfStep.ParseCondition(hrParams));
}

