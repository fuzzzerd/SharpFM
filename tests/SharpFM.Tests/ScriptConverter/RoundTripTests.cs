using System.Linq;
using System.Xml.Linq;
using SharpFM.Core.ScriptConverter;
using Xunit;

namespace SharpFM.Tests.ScriptConverter;

public class RoundTripTests
{
    private static string Wrap(string steps) =>
        $"<fmxmlsnippet type=\"FMObjectList\">{steps}</fmxmlsnippet>";

    private static void AssertStructuralEquivalence(string originalXml, string roundTrippedXml)
    {
        var original = XDocument.Parse(originalXml);
        var roundTripped = XDocument.Parse(roundTrippedXml);

        var origSteps = original.Root!.Elements("Step").ToArray();
        var rtSteps = roundTripped.Root!.Elements("Step").ToArray();

        Assert.Equal(origSteps.Length, rtSteps.Length);

        for (int i = 0; i < origSteps.Length; i++)
        {
            // Same step name
            Assert.Equal(
                origSteps[i].Attribute("name")?.Value,
                rtSteps[i].Attribute("name")?.Value);

            // Same enable state
            Assert.Equal(
                origSteps[i].Attribute("enable")?.Value ?? "True",
                rtSteps[i].Attribute("enable")?.Value ?? "True");
        }
    }

    [Fact]
    public void Comment_RoundTrips()
    {
        var xml = Wrap("<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>test comment</Text></Step>");
        var hr = XmlToHrConverter.Convert(xml);
        var result = HrToXmlConverter.Convert(hr);
        AssertStructuralEquivalence(xml, result.Xml);

        // Verify content preserved
        var step = XDocument.Parse(result.Xml).Root!.Element("Step")!;
        Assert.Equal("test comment", step.Element("Text")?.Value);
    }

    [Fact]
    public void SetVariable_RoundTrips()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[$count + 1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$count</Name></Step>");
        var hr = XmlToHrConverter.Convert(xml);
        var result = HrToXmlConverter.Convert(hr);
        AssertStructuralEquivalence(xml, result.Xml);

        var step = XDocument.Parse(result.Xml).Root!.Element("Step")!;
        Assert.Equal("$count", step.Element("Name")?.Value);
        Assert.Equal("$count + 1", step.Element("Value")?.Element("Calculation")?.Value);
    }

    [Fact]
    public void IfElseEndIf_RoundTrips()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"68\" name=\"If\"><Calculation><![CDATA[$x > 0]]></Calculation></Step>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>yes</Text></Step>"
            + "<Step enable=\"True\" id=\"69\" name=\"Else\"/>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>no</Text></Step>"
            + "<Step enable=\"True\" id=\"70\" name=\"End If\"/>");
        var hr = XmlToHrConverter.Convert(xml);
        var result = HrToXmlConverter.Convert(hr);
        AssertStructuralEquivalence(xml, result.Xml);
    }

    [Fact]
    public void SetField_RoundTrips()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Calculation><![CDATA[\"Done\"]]></Calculation>"
            + "<Field table=\"Invoices\" id=\"3\" name=\"Status\"/></Step>");
        var hr = XmlToHrConverter.Convert(xml);
        var result = HrToXmlConverter.Convert(hr);
        AssertStructuralEquivalence(xml, result.Xml);

        var step = XDocument.Parse(result.Xml).Root!.Element("Step")!;
        var field = step.Element("Field")!;
        Assert.Equal("Invoices", field.Attribute("table")?.Value);
        Assert.Equal("Status", field.Attribute("name")?.Value);
    }

    [Fact]
    public void DisabledStep_RoundTrips()
    {
        var xml = Wrap("<Step enable=\"False\" id=\"93\" name=\"Beep\"/>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.StartsWith("//", hr.TrimStart());
        var result = HrToXmlConverter.Convert(hr);
        AssertStructuralEquivalence(xml, result.Xml);
    }

    [Fact]
    public void SelfClosingSteps_RoundTrip()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"70\" name=\"End If\"/>"
            + "<Step enable=\"True\" id=\"73\" name=\"End Loop\"/>"
            + "<Step enable=\"True\" id=\"7\" name=\"New Record/Request\"/>");
        var hr = XmlToHrConverter.Convert(xml);
        var result = HrToXmlConverter.Convert(hr);
        AssertStructuralEquivalence(xml, result.Xml);
    }

    [Fact]
    public void MixedScript_RoundTrips()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>Initialize</Text></Step>"
            + "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[0]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$count</Name></Step>"
            + "<Step enable=\"True\" id=\"68\" name=\"If\"><Calculation><![CDATA[Get ( FoundCount ) > 0]]></Calculation></Step>"
            + "<Step enable=\"True\" id=\"71\" name=\"Loop\"/>"
            + "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Calculation><![CDATA[\"Processed\"]]></Calculation>"
            + "<Field table=\"Records\" id=\"0\" name=\"Status\"/></Step>"
            + "<Step enable=\"True\" id=\"141\" name=\"Set Variable\">"
            + "<Value><Calculation><![CDATA[$count + 1]]></Calculation></Value>"
            + "<Repetition><Calculation><![CDATA[1]]></Calculation></Repetition>"
            + "<Name>$count</Name></Step>"
            + "<Step enable=\"True\" id=\"72\" name=\"Exit Loop If\"><Calculation><![CDATA[$count > 100]]></Calculation></Step>"
            + "<Step enable=\"True\" id=\"73\" name=\"End Loop\"/>"
            + "<Step enable=\"True\" id=\"70\" name=\"End If\"/>"
            + "<Step enable=\"True\" id=\"89\" name=\"# (comment)\"><Text>Done</Text></Step>");
        var hr = XmlToHrConverter.Convert(xml);
        var result = HrToXmlConverter.Convert(hr);

        Assert.Empty(result.Errors);
        AssertStructuralEquivalence(xml, result.Xml);

        // Verify indentation in HR output
        var lines = hr.Split('\n');
        Assert.StartsWith("# Initialize", lines[0]);
        Assert.StartsWith("    Loop", lines[3]);
        Assert.StartsWith("        Set Field", lines[4]);
        Assert.StartsWith("        Set Variable", lines[5]);
        Assert.StartsWith("        Exit Loop If", lines[6]);
        Assert.StartsWith("    End Loop", lines[7]);
        Assert.StartsWith("End If", lines[8]);
    }

    [Fact]
    public void HrToXmlToHr_Preserves()
    {
        var original = "# Start script\n"
            + "Set Variable [ $x ; Value: 1 ]\n"
            + "If [ $x > 0 ]\n"
            + "    Set Field [ Table::Field ; \"value\" ]\n"
            + "End If";

        var xmlResult = HrToXmlConverter.Convert(original);
        Assert.Empty(xmlResult.Errors);
        var hrAgain = XmlToHrConverter.Convert(xmlResult.Xml);
        Assert.Equal(original, hrAgain);
    }

    [Fact]
    public void PerformScript_RoundTrips()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"1\" name=\"Perform Script\">"
            + "<Calculation><![CDATA[$param]]></Calculation>"
            + "<Script id=\"5\" name=\"Process Records\"/>"
            + "</Step>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Contains("\"Process Records\"", hr);
        Assert.Contains("Parameter: $param", hr);

        var result = HrToXmlConverter.Convert(hr);
        AssertStructuralEquivalence(xml, result.Xml);
    }

    [Fact]
    public void GoToLayout_Named_RoundTrips()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"SelectedLayout\"/>"
            + "<Layout id=\"3\" name=\"Invoices\"/>"
            + "</Step>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Contains("\"Invoices\"", hr);

        var result = HrToXmlConverter.Convert(hr);
        AssertStructuralEquivalence(xml, result.Xml);
    }

    [Fact]
    public void GoToLayout_Original_RoundTrips()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"OriginalLayout\"/>"
            + "</Step>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Contains("original layout", hr);

        var result = HrToXmlConverter.Convert(hr);
        var doc = XDocument.Parse(result.Xml);
        var dest = doc.Root!.Element("Step")!.Element("LayoutDestination");
        Assert.Equal("OriginalLayout", dest?.Attribute("value")?.Value);
    }

    [Fact]
    public void GoToRecord_RoundTrips()
    {
        var hr = "Go to Record/Request/Page [ Next ; Exit after last: On ]";
        var result = HrToXmlConverter.Convert(hr);
        Assert.Empty(result.Errors);
        var doc = XDocument.Parse(result.Xml);
        var step = doc.Root!.Element("Step")!;
        Assert.Equal("Next", step.Element("RowPageLocation")?.Attribute("value")?.Value);
        Assert.Equal("True", step.Element("Exit")?.Attribute("state")?.Value);

        var hrAgain = XmlToHrConverter.Convert(result.Xml);
        Assert.Contains("Next", hrAgain);
        Assert.Contains("Exit after last: On", hrAgain);
    }

    [Fact]
    public void ShowCustomDialog_RoundTrips()
    {
        var xml = Wrap(
            "<Step enable=\"True\" id=\"87\" name=\"Show Custom Dialog\">"
            + "<Title><Calculation><![CDATA[\"Warning\"]]></Calculation></Title>"
            + "<Message><Calculation><![CDATA[\"Are you sure?\"]]></Calculation></Message>"
            + "<Buttons>"
            + "<Button CommitState=\"True\"><Calculation><![CDATA[\"OK\"]]></Calculation></Button>"
            + "<Button CommitState=\"True\"><Calculation><![CDATA[\"Cancel\"]]></Calculation></Button>"
            + "</Buttons>"
            + "</Step>");
        var hr = XmlToHrConverter.Convert(xml);
        Assert.Contains("Title: \"Warning\"", hr);
        Assert.Contains("Message: \"Are you sure?\"", hr);
        Assert.Contains("Buttons: \"OK\", \"Cancel\"", hr);

        var result = HrToXmlConverter.Convert(hr);
        AssertStructuralEquivalence(xml, result.Xml);
    }

    [Fact]
    public void RealisticScript_FullRoundTrip()
    {
        // A realistic script that exercises multiple specialized renderers
        var original = "# Navigate and process records\n"
            + "Go to Layout [ \"Invoices\" ]\n"
            + "Perform Script [ \"Find Open Invoices\" ; Parameter: $status ]\n"
            + "If [ Get ( FoundCount ) > 0 ]\n"
            + "    Go to Record/Request/Page [ First ]\n"
            + "    Loop\n"
            + "        Set Field [ Invoices::Status ; \"Processed\" ]\n"
            + "        Set Variable [ $count ; Value: $count + 1 ]\n"
            + "        Go to Record/Request/Page [ Next ; Exit after last: On ]\n"
            + "    End Loop\n"
            + "    Show Custom Dialog [ Title: \"Done\" ; Message: $count & \" records processed\" ]\n"
            + "End If\n"
            + "Go to Layout [ original layout ]";

        var xmlResult = HrToXmlConverter.Convert(original);
        Assert.Empty(xmlResult.Errors);

        var hrAgain = XmlToHrConverter.Convert(xmlResult.Xml);
        Assert.Equal(original, hrAgain);
    }
}
