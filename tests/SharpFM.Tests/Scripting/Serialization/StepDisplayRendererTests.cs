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
        protected internal override void PopulateFromXml(XElement step) => throw new System.NotImplementedException();
        protected internal override void PopulateFromDisplay(string[] hrParams) => throw new System.NotImplementedException();
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

    [Fact]
    public void DisplayParser_InvertsTheRenderer()
    {
        var step = new VarStep { Name = "$arr", Value = new Calculation("0"), Repetition = new Calculation("2") };
        var line = StepDisplayRenderer.Render(step, VarMeta());
        var parsed = ScriptLineParser.ParseLine(line);
        var rebuilt = StepDisplayParser.Parse<VarStep>(true, parsed.Params, VarMeta());

        Assert.Equal("$arr", rebuilt.Name);
        Assert.Equal("0", rebuilt.Value.Text);
        Assert.Equal("2", rebuilt.Repetition.Text);
    }

    [Fact]
    public void DisplayParser_InvertedBoolean_MapsBackToWire()
    {
        var meta = new StepMetadata
        {
            Name = "Delete All Records",
            Id = 10,
            Category = "records",
            Shape =
            [
                new BoolStateChild("NoInteract") { PocoProperty = "Flag", HrLabel = "With dialog", DisplayInverted = true },
            ],
        };
        var rebuilt = StepDisplayParser.Parse<FlagStep>(true, ["With dialog: On"], meta);
        Assert.False(rebuilt.Flag); // display On = wire NoInteract False

        var shown = StepDisplayRenderer.Render(rebuilt, meta);
        Assert.Equal("Delete All Records [ With dialog: On ]", shown);
    }

    [Fact]
    public void DisplayParser_EnumDisplayForm_MapsToWireValue()
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
        var rebuilt = StepDisplayParser.Parse<VarStep>(true, ["Descending"], meta);
        Assert.Equal("SortDescending", rebuilt.Name);
    }

    [Fact]
    public void DisplayParser_BareFlagToken_SetsPresence()
    {
        var meta = new StepMetadata
        {
            Name = "Insert Text",
            Id = 61,
            Category = "fields",
            Shape = [new FlagChild("SelectAll") { PocoProperty = "Flag", HrLabel = "Select" }],
        };
        var rebuilt = StepDisplayParser.Parse<FlagStep>(true, ["Select"], meta);
        Assert.True(rebuilt.Flag);
    }

    private sealed class FlagStep : ScriptStep
    {
        public bool Flag { get; set; }
        public FlagStep() : base(true) { }
        public override XElement ToXml() => throw new System.NotImplementedException();
        public override string ToDisplayLine() => "";
        protected internal override void PopulateFromXml(XElement step) => throw new System.NotImplementedException();
        protected internal override void PopulateFromDisplay(string[] hrParams) => throw new System.NotImplementedException();
    }
}
