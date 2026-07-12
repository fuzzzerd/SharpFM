using System;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert Embedding (215). Canonical form (skill, AI reference): an optional
/// target <c>Field</c> then an <c>&lt;LLMEmbedding&gt;</c> wrapper holding the
/// account/model/input calculations; the wrapper is emitted empty when the step
/// is unconfigured.
/// </summary>
public sealed class InsertEmbeddingStep : ScriptStep<InsertEmbeddingStep>, IStepFactory
{
    public const int XmlId = 215;
    public const string XmlName = "Insert Embedding";

    public Calculation AccountName { get; set; } = new("");
    public Calculation Model { get; set; } = new("");
    public Calculation InputText { get; set; } = new("");
    public FieldRef? Target { get; set; }

    private InsertEmbeddingStep() : base(false) { }

    public InsertEmbeddingStep(
        Calculation? accountName = null,
        Calculation? model = null,
        Calculation? inputText = null,
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        AccountName = accountName ?? new Calculation("");
        Model = model ?? new Calculation("");
        InputText = inputText ?? new Calculation("");
        Target = target;
    }

    // Hand-written: FileMaker shows the Target last, but the shape must keep
    // the canonical XML order (Field before the LLMEmbedding wrapper).
    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"Account Name: {AccountName.Text}",
            $"Embedding Model: {Model.Text}",
            $"Input: {InputText.Text}",
        };
        if (Target is not null) parts.Add($"Target: {Target.ToDisplayString()}");
        return $"Insert Embedding [ {string.Join(" ; ", parts)} ]";
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        // Canonical: optional Field target, then the <LLMEmbedding> wrapper whose
        // account/model/input children are emitted only when set.
        Shape =
        [
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Target", Optional = true, Display = DisplayMode.Native },
            new WrapperChild("LLMEmbedding",
            [
                new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Augmented },
                new NamedCalcChild("Model") { PocoProperty = "Model", HrLabel = "Embedding Model", Optional = true, Display = DisplayMode.Augmented },
                new NamedCalcChild("InputText") { PocoProperty = "InputText", HrLabel = "Input", Optional = true, Display = DisplayMode.Augmented },
            ]),
        ],
    };
}
