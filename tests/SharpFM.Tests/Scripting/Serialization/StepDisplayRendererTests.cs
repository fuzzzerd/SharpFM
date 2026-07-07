using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

public class StepDisplayRendererTests
{
    private sealed class VarStep : ScriptStep
    {
        public string Name { get; set; } = "";
        public Calculation Value { get; set; } = new("");
        public Calculation Repetition { get; set; } = new("1");
        public VarStep() : base(true) { }
        public override XElement ToXml() => throw new System.NotImplementedException();
        public override string ToDisplayLine() => "";
    }

    // Name native; Value + Repetition augmented (labeled, after the natives);
    // Repetition carries default "1" so it drops when unset.
    private static StepMetadata VarMeta() => new()
    {
        Name = "Set Variable",
        Id = 141,
        Category = "control",
        Shape =
        [
            new NamedCalcChild("Value") { PocoProperty = "Value", HrLabel = "Value", Display = DisplayMode.Augmented },
            new NamedCalcChild("Repetition") { PocoProperty = "Repetition", HrLabel = "rep", Display = DisplayMode.Augmented, Optional = true, DefaultValue = "1" },
            new NamedTextChild("Name") { PocoProperty = "Name", Display = DisplayMode.Native },
        ],
    };

    [Fact]
    public void SingleBracket_DefaultRepetitionSuppressed()
    {
        var step = new VarStep { Name = "$count", Value = new Calculation("0"), Repetition = new Calculation("1") };
        Assert.Equal("Set Variable [ $count ; Value: 0 ]", StepDisplayRenderer.Render(step, VarMeta()));
    }

    [Fact]
    public void SingleBracket_NonDefaultRepetitionShown()
    {
        var step = new VarStep { Name = "$arr", Value = new Calculation("0"), Repetition = new Calculation("2") };
        Assert.Equal("Set Variable [ $arr ; Value: 0 ; rep: 2 ]", StepDisplayRenderer.Render(step, VarMeta()));
    }

    [Fact]
    public void NativeOnly_BareToken()
    {
        var meta = new StepMetadata
        {
            Name = "If",
            Id = 68,
            Category = "control",
            Shape = [new BareCalcChild { PocoProperty = "Value", Display = DisplayMode.Native }],
        };
        var step = new VarStep { Value = new Calculation("$x > 1") };
        Assert.Equal("If [ $x > 1 ]", StepDisplayRenderer.Render(step, meta));
    }

    [Fact]
    public void EnumSlot_TranslatesWireValueToDisplayForm()
    {
        var meta = new StepMetadata
        {
            Name = "Sort Records by Field",
            Id = 154,
            Category = "found-sets",
            Shape =
            [
                new EnumValueChild("SortOrder")
                {
                    PocoProperty = "Name",
                    ValidValues = ["SortAscending", "SortDescending"],
                    DisplayValues = ["Ascending", "Descending"],
                },
            ],
        };
        var step = new VarStep { Name = "SortAscending" };
        Assert.Equal("Sort Records by Field [ Ascending ]", StepDisplayRenderer.Render(step, meta));
    }

    [Fact]
    public void EmptyValues_DropTheirTokens()
    {
        var step = new VarStep { Name = "", Value = new Calculation(""), Repetition = new Calculation("1") };
        Assert.Equal("Set Variable", StepDisplayRenderer.Render(step, VarMeta()));
    }
}
