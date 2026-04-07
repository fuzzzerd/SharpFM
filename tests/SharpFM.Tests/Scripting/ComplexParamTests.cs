using Xunit;
using Xunit.Abstractions;

namespace SharpFM.Tests.Scripting;

public class ComplexParamTests
{
    private readonly ITestOutputHelper _output;

    public ComplexParamTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void ShowCustomDialog_ComplexParams_RoundTrip()
    {
        var def = StepCatalogLoader.ByName["Show Custom Dialog"];

        var testParams = new Dictionary<string, string?>
        {
            ["Title"] = "\"Enter a number\"",
            ["Message"] = "\"How many?\"",
            ["Buttons"] = "<Button CommitState=\"True\"><Calculation><![CDATA[\"OK\"]]></Calculation></Button>",
            ["InputFields"] = "<InputField><Target><Variable value=\"$n\"/></Target></InputField>"
        };

        var paramValues = def.Params.Select(p =>
        {
            var paramName = p.HrLabel ?? p.WrapperElement ?? p.XmlElement;
            testParams.TryGetValue(paramName, out var value);
            return new StepParamValue(p, value);
        }).ToList();

        var step = new ScriptStep(def, true, paramValues);
        var xml = step.ToXml();
        var xmlStr = xml.ToString();

        _output.WriteLine("=== XML ===");
        _output.WriteLine(xmlStr);

        Assert.Contains("\"Enter a number\"", xmlStr);
        Assert.Contains("\"How many?\"", xmlStr);
        Assert.Contains("<Button", xmlStr);
        Assert.Contains("<InputField", xmlStr);
        Assert.Contains("$n", xmlStr);
    }

    [Fact]
    public void ShowCustomDialog_FromXml_PreservesComplexParams()
    {
        var xml = @"<Step enable=""True"" id=""87"" name=""Show Custom Dialog"">
            <Title><Calculation><![CDATA[""Hello""]]></Calculation></Title>
            <Message><Calculation><![CDATA[""World""]]></Calculation></Message>
            <Buttons>
                <Button CommitState=""True""><Calculation><![CDATA[""OK""]]></Calculation></Button>
                <Button CommitState=""True""><Calculation><![CDATA[""Cancel""]]></Calculation></Button>
            </Buttons>
            <InputFields>
                <InputField><Target><Variable value=""$n""/></Target></InputField>
            </InputFields>
        </Step>";

        var step = ScriptStep.FromXml(System.Xml.Linq.XElement.Parse(xml));

        _output.WriteLine("=== ParamValues ===");
        foreach (var pv in step.ParamValues)
        {
            var name = pv.Definition.HrLabel ?? pv.Definition.WrapperElement ?? pv.Definition.XmlElement;
            _output.WriteLine($"  {name} ({pv.Definition.Type}) = {pv.Value ?? "(null)"}");
        }

        // Verify complex params were extracted
        var buttons = step.ParamValues.FirstOrDefault(p => p.Definition.XmlElement == "Buttons");
        var inputFields = step.ParamValues.FirstOrDefault(p => p.Definition.XmlElement == "InputFields");

        Assert.NotNull(buttons?.Value);
        Assert.Contains("<Button", buttons!.Value);
        Assert.NotNull(inputFields?.Value);
        Assert.Contains("$n", inputFields!.Value);

        // Re-serialize — always uses ParamValues (no SourceXml dependency)
        var output = step.ToXml().ToString();

        _output.WriteLine("\n=== Re-serialized XML ===");
        _output.WriteLine(output);

        Assert.Contains("<Button", output);
        Assert.Contains("<InputField", output);
        Assert.Contains("$n", output);
    }
}
