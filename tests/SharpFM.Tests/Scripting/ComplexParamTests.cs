using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace SharpFM.Tests.Scripting;

/// <summary>
/// Verifies that complex-shaped params (arbitrary nested XML children
/// like Show Custom Dialog's Buttons and InputFields) survive the
/// ingestion → emission round-trip under the new catalog-driven
/// pipeline. Complex params are extracted by
/// <see cref="CatalogParamExtractor"/> as verbatim inner-XML strings
/// and rebuilt by <see cref="CatalogXmlBuilder"/>, so any structure
/// the caller puts in comes back out unchanged.
/// </summary>
public class ComplexParamTests
{
    private readonly ITestOutputHelper _output;

    public ComplexParamTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void ShowCustomDialog_ComplexParams_BuildAndEmitPreserveContent()
    {
        var def = StepCatalogLoader.ByName["Show Custom Dialog"];

        var paramMap = new Dictionary<string, string?>
        {
            ["Title"] = "\"Enter a number\"",
            ["Message"] = "\"How many?\"",
            ["Buttons"] = "<Button CommitState=\"True\"><Calculation><![CDATA[\"OK\"]]></Calculation></Button>",
            ["InputFields"] = "<InputField><Target><Variable value=\"$n\"/></Target></InputField>"
        };

        var element = CatalogXmlBuilder.BuildStepFromMap(def, enabled: true, paramMap);
        var step = ScriptStep.FromXml(element);
        var xmlStr = step.ToXml().ToString();

        _output.WriteLine("=== Emitted XML ===");
        _output.WriteLine(xmlStr);

        Assert.Contains("\"Enter a number\"", xmlStr);
        Assert.Contains("\"How many?\"", xmlStr);
        Assert.Contains("<Button", xmlStr);
        Assert.Contains("<InputField", xmlStr);
        Assert.Contains("$n", xmlStr);
    }

    [Fact]
    public void ShowCustomDialog_FromXml_PreservesComplexChildrenThroughRoundTrip()
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

        var step = ScriptStep.FromXml(XElement.Parse(xml));
        var output = step.ToXml().ToString();

        _output.WriteLine("=== Re-serialized XML ===");
        _output.WriteLine(output);

        // The Buttons and InputFields nested structures must survive
        // parse → emit without loss. The RawStep holds the source
        // element verbatim, so re-serialization is byte-identical
        // modulo whitespace.
        Assert.Contains("<Button", output);
        Assert.Contains("\"OK\"", output);
        Assert.Contains("\"Cancel\"", output);
        Assert.Contains("<InputField", output);
        Assert.Contains("$n", output);
    }
}
