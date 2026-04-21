using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertFromUrlStepTests
{
    private const string CanonicalXml = """
        <Step enable="True" id="160" name="Insert from URL"><NoInteract state="True" /><DontEncodeURL state="False" /><SelectAll state="True" /><VerifySSLCertificates state="True" /><CURLOptions><Calculation><![CDATA["--flags"]]></Calculation></CURLOptions><Calculation><![CDATA[$url]]></Calculation><Text /><Field>$file</Field></Step>
        """;

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertFromUrlStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert from URL", out var metadata));
        Assert.Equal(160, metadata!.Id);
    }

    [Theory]
    [InlineData(false, false, true, false, false, false, false)]
    [InlineData(true, false, false, false, false, false, false)]
    [InlineData(false, false, true, false, true, true, true)]
    [InlineData(true, false, true, false, true, true, true)]
    [InlineData(true, false, true, true, true, true, true)]
    [InlineData(true, true, true, true, true, true, true)]
    public void RealWorldShapes_RoundTrip(
        bool noInteract, bool dontEncode, bool selectAll, bool verifySsl,
        bool withCurlOptions, bool withUrl, bool withField)
    {
        // Covers the series of variants in the user's FM Pro sample: base,
        // base without Select, with calcs, set to variable, verify SSL on,
        // and DontEncodeURL on. Each must XML-round-trip byte-intact.
        var xml = new System.Text.StringBuilder();
        xml.Append("<Step enable=\"True\" id=\"160\" name=\"Insert from URL\">");
        xml.Append($"<NoInteract state=\"{(noInteract ? "True" : "False")}\" />");
        xml.Append($"<DontEncodeURL state=\"{(dontEncode ? "True" : "False")}\" />");
        xml.Append($"<SelectAll state=\"{(selectAll ? "True" : "False")}\" />");
        xml.Append($"<VerifySSLCertificates state=\"{(verifySsl ? "True" : "False")}\" />");
        if (withCurlOptions)
            xml.Append("<CURLOptions><Calculation><![CDATA[\"--user myuser:mypass\"]]></Calculation></CURLOptions>");
        if (withUrl)
            xml.Append("<Calculation><![CDATA[\"https://example.com/\" & $id]]></Calculation>");
        if (withField)
        {
            xml.Append("<Text />");
            xml.Append("<Field>$insertFromUrlDataResponseVar</Field>");
        }
        xml.Append("</Step>");

        var source = XElement.Parse(xml.ToString());
        var step = InsertFromUrlStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()),
            $"Round-trip mismatch.\nSource:\n{source}\n\nOutput:\n{step.ToXml()}");
    }

    [Fact]
    public void Display_BaseShape_IsValidatorClean()
    {
        // Sanity: render the display line for a typical shape and feed
        // it back through the script validator — zero diagnostics expected.
        var source = XElement.Parse(
            "<Step enable=\"True\" id=\"160\" name=\"Insert from URL\">"
            + "<NoInteract state=\"False\" /><DontEncodeURL state=\"False\" />"
            + "<SelectAll state=\"True\" /><VerifySSLCertificates state=\"False\" />"
            + "</Step>");
        var step = InsertFromUrlStep.Metadata.FromXml!(source);
        var display = step.ToDisplayLine();
        var diagnostics = SharpFM.Model.Scripting.ScriptValidator.Validate(display);
        Assert.Empty(diagnostics);
    }
}
