using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed POCOs for FileMaker's block-pair control-flow steps:
/// <c>If</c>, <c>Else If</c>, <c>Else</c>, <c>End If</c>, <c>Loop</c>,
/// <c>End Loop</c>, <c>Exit Loop If</c>. Serialization is shape-driven
/// (<see cref="StepXmlRenderer"/> / <see cref="StepXmlParser"/>).
/// <para>
/// Canonical-form note: per the vendored FileMaker XML skill (§8.1), FileMaker
/// Pro <em>does</em> emit <c>&lt;Restore state="False"/&gt;</c> on
/// <c>If</c> / <c>Else If</c> / <c>Else</c> / <c>Loop</c>, and
/// <c>&lt;FlushType value="Always"/&gt;</c> on <c>Loop</c>. These are paste-clean
/// canonical elements, so SharpFM emits them. They carry fixed values FileMaker
/// never varies, so they are hidden from the display line but round-tripped in
/// XML. (Earlier revisions dropped them on the belief FM never wrote them; the
/// skill's round-trip testing against FM Pro 2025/2026 supersedes that.)
/// </para>
/// </summary>
public sealed class IfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 68;
    public const string XmlName = "If";

    public Calculation Condition { get; set; } = new("");

    /// <summary>The canonical <c>&lt;Restore state="False"/&gt;</c> flag (skill §8.1); fixed-value, XML-only.</summary>
    public bool Restore { get; set; }

    private IfStep() : base(false) { }

    public IfStep(bool enabled, Calculation condition)
        : base(enabled)
    {
        Condition = condition;
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<IfStep>(step, Metadata);

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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
        // Canonical §8.1: <Restore state="False"/> then <Calculation>.
        Shape =
        [
            new BoolStateChild("Restore") { PocoProperty = "Restore", Display = DisplayMode.Hidden },
            new BareCalcChild { PocoProperty = "Condition", Required = true, Display = DisplayMode.Native },
        ],
        Notes = new StepNotes { Constraints = "Requires a matching End If step." },
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };

    internal static Calculation ParseCondition(string[] hrParams) =>
        hrParams.Length >= 1 ? new Calculation(hrParams[0].Trim()) : new Calculation("");
}

public sealed class ElseIfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 125;
    public const string XmlName = "Else If";

    public Calculation Condition { get; set; } = new("");

    /// <summary>The canonical <c>&lt;Restore state="False"/&gt;</c> flag (skill §8.1); fixed-value, XML-only.</summary>
    public bool Restore { get; set; }

    private ElseIfStep() : base(false) { }

    public ElseIfStep(bool enabled, Calculation condition)
        : base(enabled)
    {
        Condition = condition;
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ElseIfStep>(step, Metadata);

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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
        // Canonical §8.1: <Restore state="False"/> then <Calculation>.
        Shape =
        [
            new BoolStateChild("Restore") { PocoProperty = "Restore", Display = DisplayMode.Hidden },
            new BareCalcChild { PocoProperty = "Condition", Required = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}

public sealed class ElseStep : ScriptStep, IStepFactory
{
    public const int XmlId = 69;
    public const string XmlName = "Else";

    /// <summary>The canonical <c>&lt;Restore state="False"/&gt;</c> flag (skill §8.1); fixed-value, XML-only.</summary>
    public bool Restore { get; set; }

    private ElseStep() : base(false) { }

    public ElseStep(bool enabled)
        : base(enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ElseStep>(step, Metadata);

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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
        // Canonical §8.1: <Restore state="False"/>.
        Shape = [new BoolStateChild("Restore") { PocoProperty = "Restore", Display = DisplayMode.Hidden }],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}

public sealed class EndIfStep : ScriptStep, IStepFactory
{
    public const int XmlId = 70;
    public const string XmlName = "End If";

    private EndIfStep() : base(false) { }

    public EndIfStep(bool enabled)
        : base(enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<EndIfStep>(step, Metadata);

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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

    /// <summary>The canonical <c>&lt;Restore state="False"/&gt;</c> flag (skill §8.1); fixed-value, XML-only.</summary>
    public bool Restore { get; set; }

    /// <summary>The canonical <c>&lt;FlushType value="Always"/&gt;</c> flag (skill §8.1); fixed-value, XML-only.</summary>
    public string FlushType { get; set; } = "Always";

    private LoopStep() : base(false) { }

    public LoopStep(bool enabled)
        : base(enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<LoopStep>(step, Metadata);

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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
        // Canonical §8.1: <Restore state="False"/> then <FlushType value="Always"/>.
        Shape =
        [
            new BoolStateChild("Restore") { PocoProperty = "Restore", Display = DisplayMode.Hidden },
            new EnumValueChild("FlushType") { PocoProperty = "FlushType", DefaultValue = "Always", Display = DisplayMode.Hidden },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}

public sealed class EndLoopStep : ScriptStep, IStepFactory
{
    public const int XmlId = 73;
    public const string XmlName = "End Loop";

    private EndLoopStep() : base(false) { }

    public EndLoopStep(bool enabled)
        : base(enabled) { }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<EndLoopStep>(step, Metadata);

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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

    public Calculation Condition { get; set; } = new("");

    private ExitLoopIfStep() : base(false) { }

    public ExitLoopIfStep(bool enabled, Calculation condition)
        : base(enabled)
    {
        Condition = condition;
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ExitLoopIfStep>(step, Metadata);

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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
        // Canonical §8.1: bare <Calculation> only — no <Restore> on Exit Loop If.
        Shape = [new BareCalcChild { PocoProperty = "Condition", Required = true, Display = DisplayMode.Native }],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
