using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed POCOs for FileMaker's block-pair control-flow steps:
/// <c>If</c>, <c>Else If</c>, <c>Else</c>, <c>End If</c>, <c>Loop</c>,
/// <c>End Loop</c>, <c>Exit Loop If</c>.
/// <para>
/// Every step here is an <see cref="IStepFactory"/>. The single-file
/// layout keeps the related steps and their shared helpers
/// (<see cref="IfStep.BuildConditionedStep"/>, <see cref="ElseStep.BuildBareStep"/>)
/// colocated.
/// </para>
/// <para>
/// Zero-loss audit:
/// <list type="bullet">
/// <item><c>If</c> / <c>Else If</c> / <c>Loop</c> carry a <c>Restore</c> element in some upstream XML
/// sources. FM Pro never writes it and never changes the value; we drop it on read and never emit it.
/// See <c>docs/advanced-filemaker-scripting-syntax.md</c>.</item>
/// <item><c>Loop</c>'s <c>FlushType</c> enum is also absent from FM Pro clipboard output. Not surfaced
/// in display and not round-tripped — same "semantically fixed / never-changed" justification.</item>
/// </list>
/// </para>
/// </summary>
public sealed class IfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 68;
    public const string XmlName = "If";

    public Calculation Condition { get; set; }

    public IfStep(bool enabled, Calculation condition)
        : base(BuildLegacyDefinition(XmlName, XmlId, BlockPairRole.Open, ["Else", "Else If", "End If"]), enabled)
    {
        Condition = condition;
    }

    // Legacy consumers (FmScript.ToDisplayLines) still read step.Definition.BlockPair
    // for indentation. Synthesize just enough StepDefinition to satisfy them until
    // those consumers migrate to StepRegistry.
    internal static StepDefinition BuildLegacyDefinition(string name, int id, BlockPairRole role, string[] partners) => new()
    {
        Name = name,
        Id = id,
        BlockPair = new StepBlockPair { Role = role, Partners = partners },
    };

    public static new ScriptStep FromXml(XElement step) =>
        new IfStep(
            step.Attribute("enable")?.Value != "False",
            ReadCalculation(step));

    public override XElement ToXml() =>
        BuildConditionedStep(this, XmlName, XmlId, Condition);

    public override string ToDisplayLine() => $"If [ {Condition.Text} ]";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new IfStep(enabled, ParseCondition(hrParams));

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
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
                Required = true,
            },
        ],
        Notes = new StepNotes { Constraints = "Requires a matching End If step." },
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

public sealed class ElseIfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 125;
    public const string XmlName = "Else If";

    public Calculation Condition { get; set; }

    public ElseIfStep(bool enabled, Calculation condition)
        : base(IfStep.BuildLegacyDefinition(XmlName, XmlId, BlockPairRole.Middle, ["If", "End If"]), enabled)
    {
        Condition = condition;
    }

    public static new ScriptStep FromXml(XElement step) =>
        new ElseIfStep(
            step.Attribute("enable")?.Value != "False",
            IfStep.ReadCalculation(step));

    public override XElement ToXml() =>
        IfStep.BuildConditionedStep(this, XmlName, XmlId, Condition);

    public override string ToDisplayLine() => $"Else If [ {Condition.Text} ]";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ElseIfStep(enabled, IfStep.ParseCondition(hrParams));

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/else-if.html",
        HrSignature = "[ condition ]",
        BlockPair = new StepBlockPair
        {
            Role = BlockPairRole.Middle,
            Partners = ["If", "End If"],
        },
        Params =
        [
            new ParamMetadata
            {
                Name = "condition",
                XmlElement = "Calculation",
                Type = "calculation",
                Required = true,
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}

public sealed class ElseStep : ScriptStep, IStepFactory
{
    public const int XmlId = 69;
    public const string XmlName = "Else";

    public ElseStep(bool enabled)
        : base(IfStep.BuildLegacyDefinition(XmlName, XmlId, BlockPairRole.Middle, ["If", "End If"]), enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        new ElseStep(step.Attribute("enable")?.Value != "False");

    public override XElement ToXml() => BuildBareStep(this, XmlName, XmlId);

    public override string ToDisplayLine() => XmlName;

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ElseStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/else.html",
        HrSignature = "Else",
        BlockPair = new StepBlockPair
        {
            Role = BlockPairRole.Middle,
            Partners = ["If", "End If"],
        },
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };

    internal static XElement BuildBareStep(ScriptStep owner, string name, int id) =>
        new("Step",
            new XAttribute("enable", owner.Enabled ? "True" : "False"),
            new XAttribute("id", id),
            new XAttribute("name", name));
}

public sealed class EndIfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 70;
    public const string XmlName = "End If";

    public EndIfStep(bool enabled)
        : base(IfStep.BuildLegacyDefinition(XmlName, XmlId, BlockPairRole.Close, ["If"]), enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        new EndIfStep(step.Attribute("enable")?.Value != "False");

    public override XElement ToXml() => ElseStep.BuildBareStep(this, XmlName, XmlId);

    public override string ToDisplayLine() => XmlName;

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new EndIfStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/end-if.html",
        HrSignature = "End If",
        BlockPair = new StepBlockPair
        {
            Role = BlockPairRole.Close,
            Partners = ["If"],
        },
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}

public sealed class LoopStep : ScriptStep, IStepFactory
{
    public const int XmlId = 71;
    public const string XmlName = "Loop";

    public LoopStep(bool enabled)
        : base(IfStep.BuildLegacyDefinition(XmlName, XmlId, BlockPairRole.Open, ["End Loop"]), enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        new LoopStep(step.Attribute("enable")?.Value != "False");

    public override XElement ToXml() => ElseStep.BuildBareStep(this, XmlName, XmlId);

    public override string ToDisplayLine() => XmlName;

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new LoopStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/loop.html",
        HrSignature = "Loop",
        BlockPair = new StepBlockPair
        {
            Role = BlockPairRole.Open,
            Partners = ["End Loop"],
        },
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}

public sealed class EndLoopStep : ScriptStep, IStepFactory
{
    public const int XmlId = 73;
    public const string XmlName = "End Loop";

    public EndLoopStep(bool enabled)
        : base(IfStep.BuildLegacyDefinition(XmlName, XmlId, BlockPairRole.Close, ["Loop"]), enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        new EndLoopStep(step.Attribute("enable")?.Value != "False");

    public override XElement ToXml() => ElseStep.BuildBareStep(this, XmlName, XmlId);

    public override string ToDisplayLine() => XmlName;

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new EndLoopStep(enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/end-loop.html",
        HrSignature = "End Loop",
        BlockPair = new StepBlockPair
        {
            Role = BlockPairRole.Close,
            Partners = ["Loop"],
        },
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}

public sealed class ExitLoopIfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 72;
    public const string XmlName = "Exit Loop If";

    public Calculation Condition { get; set; }

    public ExitLoopIfStep(bool enabled, Calculation condition)
        : base(IfStep.BuildLegacyDefinition(XmlName, XmlId, BlockPairRole.Middle, ["Loop", "End Loop"]), enabled)
    {
        Condition = condition;
    }

    public static new ScriptStep FromXml(XElement step) =>
        new ExitLoopIfStep(
            step.Attribute("enable")?.Value != "False",
            IfStep.ReadCalculation(step));

    public override XElement ToXml() =>
        IfStep.BuildConditionedStep(this, XmlName, XmlId, Condition);

    public override string ToDisplayLine() => $"Exit Loop If [ {Condition.Text} ]";

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new ExitLoopIfStep(enabled, IfStep.ParseCondition(hrParams));

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/exit-loop-if.html",
        HrSignature = "[ condition ]",
        BlockPair = new StepBlockPair
        {
            Role = BlockPairRole.Middle,
            Partners = ["Loop", "End Loop"],
        },
        Params =
        [
            new ParamMetadata
            {
                Name = "condition",
                XmlElement = "Calculation",
                Type = "calculation",
                Required = true,
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
