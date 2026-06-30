using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert Embedding (215). Canonical form (skill, AI reference): an optional
/// target <c>Field</c> then an <c>&lt;LLMEmbedding&gt;</c> wrapper holding the
/// account/model/input calculations; the wrapper is emitted empty when the step
/// is unconfigured.
/// </summary>
public sealed class InsertEmbeddingStep : ScriptStep, IStepFactory
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

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

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertEmbeddingStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        Calculation account = new(""), model = new(""), input = new("");
        FieldRef? target = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase))
                account = new Calculation(t.Substring(13).Trim());
            else if (t.StartsWith("Embedding Model:", StringComparison.OrdinalIgnoreCase))
                model = new Calculation(t.Substring(16).Trim());
            else if (t.StartsWith("Input:", StringComparison.OrdinalIgnoreCase))
                input = new Calculation(t.Substring(6).Trim());
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
        }
        return new InsertEmbeddingStep(account, model, input, target, enabled);
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
            new FieldChild("Field") { PocoProperty = "Target", Optional = true, Display = DisplayMode.Native },
            new WrapperChild("LLMEmbedding",
            [
                new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Augmented },
                new NamedCalcChild("Model") { PocoProperty = "Model", HrLabel = "Embedding Model", Optional = true, Display = DisplayMode.Augmented },
                new NamedCalcChild("InputText") { PocoProperty = "InputText", HrLabel = "Input", Optional = true, Display = DisplayMode.Augmented },
            ]),
        ],
        Params =
        [
            new ParamMetadata { Name = "AccountName", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Account Name", Required = true },
            new ParamMetadata { Name = "Model", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Embedding Model", Required = true },
            new ParamMetadata { Name = "InputText", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Input", Required = true },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "fieldOrVariable", HrLabel = "Target" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
