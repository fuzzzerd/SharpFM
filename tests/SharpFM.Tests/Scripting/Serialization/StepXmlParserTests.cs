using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;
using SharpFM.Tests.CanonicalSkill;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

public class StepXmlParserTests
{
    // Mirrors Set Variable's shape on a step with a parameterless ctor so the
    // shape-driven parser can construct it by reflection.
    private sealed class VarLikeStep : ScriptStep
    {
        public string Name { get; set; } = "";
        public Calculation Value { get; set; } = new("");
        public Calculation Repetition { get; set; } = new("1");
        public VarLikeStep() : base(true) { }
        public override XElement ToXml() => StepXmlRenderer.Render(this, Meta);
        public override string ToDisplayLine() => "";

        public static StepMetadata Meta { get; } = new()
        {
            Name = "Set Variable",
            Id = 141,
            Category = "control",
            Shape =
            [
                new NamedCalcChild("Value") { PocoProperty = "Value" },
                new NamedCalcChild("Repetition") { PocoProperty = "Repetition" },
                new NamedTextChild("Name") { PocoProperty = "Name" },
            ],
        };
    }

    [Fact]
    public void Parses_SetVariable_PopulatesProperties()
    {
        var canonical = CanonicalSkillFixtures.Load("141-SetVariable");
        var step = StepXmlParser.Parse<VarLikeStep>(canonical, VarLikeStep.Meta);

        Assert.True(step.Enabled);
        Assert.Equal("$variable_name", step.Name);
        Assert.Equal("expression", step.Value.Text);
        Assert.Equal("1", step.Repetition.Text);
    }

    [Fact]
    public void RoundTrips_SetVariable_ParseThenRender()
    {
        var canonical = CanonicalSkillFixtures.Load("141-SetVariable");
        var step = StepXmlParser.Parse<VarLikeStep>(canonical, VarLikeStep.Meta);
        var emitted = step.ToXml();

        Assert.True(StructuralXml.Equal(canonical, emitted, out var why), why);
    }

    [Fact]
    public void Parses_DisabledStep_FromEnableAttr()
    {
        var xml = XElement.Parse(
            "<Step enable=\"False\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$x</Name></Step>");
        var step = StepXmlParser.Parse<VarLikeStep>(xml, VarLikeStep.Meta);
        Assert.False(step.Enabled);
    }

    // Mirrors Go to Layout: a value-discriminated union. The discriminator
    // element is bound to the union's computed WireValue (get-only), so it is
    // emit-only — parse selects the case from MatchElement/MatchValues instead.
    private sealed class LayoutLikeStep : ScriptStep
    {
        public LayoutTarget Target { get; set; } = new LayoutTarget.Original();
        public LayoutLikeStep() : base(true) { }
        public override XElement ToXml() => StepXmlRenderer.Render(this, Meta);
        public override string ToDisplayLine() => "";

        public static StepMetadata Meta { get; } = new()
        {
            Name = "Go to Layout",
            Id = 6,
            Category = "navigation",
            Shape =
            [
                new VariantBlock(
                [
                    new VariantCase(typeof(LayoutTarget.Original),
                        [new EnumValueChild("LayoutDestination") { PocoProperty = "WireValue" }])
                    { MatchElement = "LayoutDestination", MatchValues = ["OriginalLayout"] },
                    new VariantCase(typeof(LayoutTarget.ByNameCalc),
                        [
                            new EnumValueChild("LayoutDestination") { PocoProperty = "WireValue" },
                            new NamedCalcChild("Layout") { PocoProperty = "Calc" },
                        ])
                    { MatchElement = "LayoutDestination", MatchValues = ["LayoutNameByCalc", "LayoutNameByCalculation"] },
                    new VariantCase(typeof(LayoutTarget.Named),
                        [
                            new EnumValueChild("LayoutDestination") { PocoProperty = "WireValue" },
                            new NamedRefChild("Layout"),
                        ])
                    { MatchElement = "LayoutDestination" },
                ]) { PocoProperty = "Target" },
            ],
        };
    }

    [Fact]
    public void VariantBlock_Parses_ValueDiscriminatedCase()
    {
        var xml = XElement.Parse(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"OriginalLayout\"/></Step>");
        var step = StepXmlParser.Parse<LayoutLikeStep>(xml, LayoutLikeStep.Meta);
        Assert.IsType<LayoutTarget.Original>(step.Target);
    }

    [Fact]
    public void VariantBlock_Parses_LegacyWireValueAlias()
    {
        var xml = XElement.Parse(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"LayoutNameByCalculation\"/>"
            + "<Layout><Calculation><![CDATA[\"List View\"]]></Calculation></Layout></Step>");
        var step = StepXmlParser.Parse<LayoutLikeStep>(xml, LayoutLikeStep.Meta);
        var byName = Assert.IsType<LayoutTarget.ByNameCalc>(step.Target);
        Assert.Equal("\"List View\"", byName.Calc.Text);
    }

    [Fact]
    public void VariantBlock_FallsThrough_ToPresenceOnlyCase()
    {
        var xml = XElement.Parse(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"SelectedLayout\"/>"
            + "<Layout id=\"3\" name=\"Detail\"/></Step>");
        var step = StepXmlParser.Parse<LayoutLikeStep>(xml, LayoutLikeStep.Meta);
        var named = Assert.IsType<LayoutTarget.Named>(step.Target);
        Assert.Equal(3, named.Layout.Id);
        Assert.Equal("Detail", named.Layout.Name);
    }

    [Fact]
    public void VariantBlock_RoundTrips_ParseThenRender()
    {
        var xml = XElement.Parse(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"LayoutNumberByCalc\"/>"
            + "<Layout><Calculation><![CDATA[2 + 1]]></Calculation></Layout></Step>");
        var meta = new StepMetadata
        {
            Name = "Go to Layout",
            Id = 6,
            Category = "navigation",
            Shape =
            [
                new VariantBlock(
                [
                    new VariantCase(typeof(LayoutTarget.ByNumberCalc),
                        [
                            new EnumValueChild("LayoutDestination") { PocoProperty = "WireValue" },
                            new NamedCalcChild("Layout") { PocoProperty = "Calc" },
                        ])
                    { MatchElement = "LayoutDestination", MatchValues = ["LayoutNumberByCalc"] },
                ]) { PocoProperty = "Target" },
            ],
        };
        var step = StepXmlParser.Parse<LayoutLikeStep>(xml, meta);
        var emitted = StepXmlRenderer.Render(step, meta);
        Assert.True(StructuralXml.Equal(xml, emitted, out var why), why);
    }

    // Mirrors Perform Script: a presence-discriminated union — the XML carries
    // either <Script> or <Calculated>, never both.
    private sealed class PerformLikeStep : ScriptStep
    {
        public PerformScriptTarget Target { get; set; } = new PerformScriptTarget.ByReference(new NamedRef(0, ""));
        public PerformLikeStep() : base(true) { }
        public override XElement ToXml() => StepXmlRenderer.Render(this, Meta);
        public override string ToDisplayLine() => "";

        public static StepMetadata Meta { get; } = new()
        {
            Name = "Perform Script",
            Id = 1,
            Category = "control",
            Shape =
            [
                new VariantBlock(
                [
                    new VariantCase(typeof(PerformScriptTarget.ByReference),
                        [new NamedRefChild("Script")]) { MatchElement = "Script" },
                    new VariantCase(typeof(PerformScriptTarget.ByCalculation),
                        [new NamedCalcChild("Calculated") { PocoProperty = "NameCalc" }])
                    { MatchElement = "Calculated" },
                ]) { PocoProperty = "Target" },
            ],
        };
    }

    [Fact]
    public void VariantBlock_Parses_PresenceDiscriminatedCases()
    {
        var byRef = StepXmlParser.Parse<PerformLikeStep>(XElement.Parse(
            "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
            + "<Script id=\"7\" name=\"Startup\"/></Step>"), PerformLikeStep.Meta);
        var reference = Assert.IsType<PerformScriptTarget.ByReference>(byRef.Target);
        Assert.Equal(7, reference.Script.Id);

        var byCalc = StepXmlParser.Parse<PerformLikeStep>(XElement.Parse(
            "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
            + "<Calculated><Calculation><![CDATA[$script]]></Calculation></Calculated></Step>"), PerformLikeStep.Meta);
        var calculated = Assert.IsType<PerformScriptTarget.ByCalculation>(byCalc.Target);
        Assert.Equal("$script", calculated.NameCalc.Text);
    }

    [Fact]
    public void VariantBlock_NoMatchingCase_LeavesPocoDefault()
    {
        var step = StepXmlParser.Parse<PerformLikeStep>(XElement.Parse(
            "<Step enable=\"True\" id=\"1\" name=\"Perform Script\"/>"), PerformLikeStep.Meta);
        Assert.IsType<PerformScriptTarget.ByReference>(step.Target);
    }

    // Mirrors Perform JavaScript in Web Viewer: <Parameters Count="N"> holding
    // <P><Calculation> children bound to a List<Calculation> property.
    private sealed class JsLikeStep : ScriptStep
    {
        public List<Calculation> Parameters { get; set; } = [];
        public JsLikeStep() : base(true) { }
        public override XElement ToXml() => StepXmlRenderer.Render(this, Meta);
        public override string ToDisplayLine() => "";

        public static StepMetadata Meta { get; } = new()
        {
            Name = "Perform JavaScript in Web Viewer",
            Id = 175,
            Category = "misc",
            Shape = [new ParametersList()],
        };
    }

    // Mirrors Insert PDF: one element carrying text content plus a type
    // attribute, each bound to its own property.
    private sealed class PathLikeStep : ScriptStep
    {
        public string Path { get; set; } = "";
        public string StorageType { get; set; } = "Embedded";
        public PathLikeStep() : base(true) { }
        public override XElement ToXml() => StepXmlRenderer.Render(this, Meta);
        public override string ToDisplayLine() => "";

        public static StepMetadata Meta { get; } = new()
        {
            Name = "Insert PDF",
            Id = 158,
            Category = "fields",
            Shape =
            [
                new NamedTextChild("UniversalPathList")
                {
                    PocoProperty = "Path",
                    Attr = "type",
                    AttrProperty = "StorageType",
                    AttrDefault = "Embedded",
                },
            ],
        };
    }

    [Fact]
    public void NamedTextChild_RoundTrips_AttributeAndText()
    {
        var step = new PathLikeStep { Path = "image:pic.pdf", StorageType = "Reference" };
        var emitted = step.ToXml();
        Assert.Equal("Reference", emitted.Element("UniversalPathList")?.Attribute("type")?.Value);
        Assert.Equal("image:pic.pdf", emitted.Element("UniversalPathList")?.Value);

        var parsed = StepXmlParser.Parse<PathLikeStep>(emitted, PathLikeStep.Meta);
        Assert.Equal("image:pic.pdf", parsed.Path);
        Assert.Equal("Reference", parsed.StorageType);
    }

    [Fact]
    public void NamedTextChild_AbsentElement_UsesAttrDefault()
    {
        var parsed = StepXmlParser.Parse<PathLikeStep>(
            XElement.Parse("<Step enable=\"True\" id=\"158\" name=\"Insert PDF\"/>"),
            PathLikeStep.Meta);
        Assert.Equal("", parsed.Path);
        Assert.Equal("Embedded", parsed.StorageType);
    }

    // Mirrors Save a Copy as XML's <SaXML>: a wrapper emitted only when its
    // gate property is non-null.
    private sealed class GatedWrapperStep : ScriptStep
    {
        public Calculation? JsonOptions { get; set; }
        public GatedWrapperStep() : base(true) { }
        public override XElement ToXml() => StepXmlRenderer.Render(this, Meta);
        public override string ToDisplayLine() => "";

        public static StepMetadata Meta { get; } = new()
        {
            Name = "Save a Copy as XML",
            Id = 3,
            Category = "files",
            Shape =
            [
                new WrapperChild("SaXML",
                [
                    new BareCalcChild { PocoProperty = "JsonOptions" },
                ]) { PocoProperty = "JsonOptions", Optional = true },
            ],
        };
    }

    [Fact]
    public void OptionalWrapper_OmittedWhenGateNull_EmittedWhenSet()
    {
        Assert.Null(new GatedWrapperStep().ToXml().Element("SaXML"));

        var configured = new GatedWrapperStep { JsonOptions = new Calculation("JSONGetElement ( $$x ; \"a\" )") };
        var emitted = configured.ToXml();
        Assert.NotNull(emitted.Element("SaXML"));

        var parsed = StepXmlParser.Parse<GatedWrapperStep>(emitted, GatedWrapperStep.Meta);
        Assert.Equal(configured.JsonOptions.Text, parsed.JsonOptions?.Text);
    }

    // Mirrors the AI/ML bag steps: the whole body round-trips through a
    // StepChildBag-typed Passthrough.
    private sealed class BagLikeStep : ScriptStep
    {
        public StepChildBag Children { get; set; } = new();
        public BagLikeStep() : base(true) { }
        public override XElement ToXml() => StepXmlRenderer.Render(this, Meta);
        public override string ToDisplayLine() => "";

        public static StepMetadata Meta { get; } = new()
        {
            Name = "Perform RAG Action",
            Id = 232,
            Category = "misc",
            Shape = [new Passthrough { PocoProperty = "Children" }],
        };
    }

    [Fact]
    public void Passthrough_RoundTrips_StepChildBag()
    {
        var xml = XElement.Parse(
            "<Step enable=\"True\" id=\"232\" name=\"Perform RAG Action\">"
            + "<Account><Calculation><![CDATA[\"rag\"]]></Calculation></Account>"
            + "<Prompt><Calculation><![CDATA[$q]]></Calculation></Prompt></Step>");
        var parsed = StepXmlParser.Parse<BagLikeStep>(xml, BagLikeStep.Meta);
        Assert.Equal(2, parsed.Children.Children.Count);

        var emitted = parsed.ToXml();
        Assert.True(StructuralXml.Equal(xml, emitted, out var why), why);
    }

    [Fact]
    public void ParametersList_RoundTrips_CalculationItems()
    {
        var step = new JsLikeStep
        {
            Parameters = [new Calculation("\"arg1\""), new Calculation("$two")],
        };
        var emitted = step.ToXml();
        var parsed = StepXmlParser.Parse<JsLikeStep>(emitted, JsLikeStep.Meta);

        Assert.Equal(2, parsed.Parameters.Count);
        Assert.Equal("\"arg1\"", parsed.Parameters[0].Text);
        Assert.Equal("$two", parsed.Parameters[1].Text);

        var reEmitted = parsed.ToXml();
        Assert.True(StructuralXml.Equal(emitted, reEmitted, out var why), why);
    }
}
