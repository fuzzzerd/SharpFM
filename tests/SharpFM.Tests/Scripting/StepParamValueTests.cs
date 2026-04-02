using System.Xml.Linq;
using SharpFM.Scripting.Catalog;
using SharpFM.Scripting.Model;
using Xunit;

namespace SharpFM.Tests.Scripting;

public class StepParamValueTests
{
    private static StepParam MakeParam(string type, string xmlElement, string? hrLabel = null,
        string? xmlAttr = null, string? wrapperElement = null, string? parentElement = null,
        Dictionary<string, string?>? hrEnumValues = null, string[]? hrValues = null,
        bool invertedHr = false, string? defaultValue = null)
    {
        return new StepParam
        {
            Type = type,
            XmlElement = xmlElement,
            HrLabel = hrLabel,
            XmlAttr = xmlAttr,
            WrapperElement = wrapperElement,
            ParentElement = parentElement,
            HrEnumValues = hrEnumValues,
            HrValues = hrValues,
            InvertedHr = invertedHr,
            DefaultValue = defaultValue
        };
    }

    // --- FromXml extraction ---

    [Fact]
    public void FromXml_Calculation_ExtractsValue()
    {
        var param = MakeParam("calculation", "Calculation");
        var step = XElement.Parse("<Step><Calculation>Get(AccountName)</Calculation></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("Get(AccountName)", result.Value);
    }

    [Fact]
    public void FromXml_Calculation_Empty_ReturnsNull()
    {
        var param = MakeParam("calculation", "Calculation");
        var step = XElement.Parse("<Step><Calculation></Calculation></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Null(result.Value);
    }

    [Fact]
    public void FromXml_Text_ExtractsValue()
    {
        var param = MakeParam("text", "Text");
        var step = XElement.Parse("<Step><Text>hello world</Text></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("hello world", result.Value);
    }

    [Fact]
    public void FromXml_Boolean_True_ReturnsOn()
    {
        var param = MakeParam("boolean", "Restore");
        var step = XElement.Parse("<Step><Restore state=\"True\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("On", result.Value);
    }

    [Fact]
    public void FromXml_Boolean_False_ReturnsOff()
    {
        var param = MakeParam("boolean", "Restore");
        var step = XElement.Parse("<Step><Restore state=\"False\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("Off", result.Value);
    }

    [Fact]
    public void FromXml_Boolean_InvertedHr()
    {
        var param = MakeParam("boolean", "NoDialog", invertedHr: true);
        var step = XElement.Parse("<Step><NoDialog state=\"True\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("Off", result.Value);
    }

    [Fact]
    public void FromXml_Boolean_WithHrValues()
    {
        var param = MakeParam("boolean", "Select", hrValues: ["All", "None"]);
        var step = XElement.Parse("<Step><Select state=\"True\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("All", result.Value);
    }

    [Fact]
    public void FromXml_Boolean_WithHrEnumValues()
    {
        var param = MakeParam("boolean", "Pause", hrEnumValues: new() { { "True", "Indefinitely" }, { "False", "Off" } });
        var step = XElement.Parse("<Step><Pause state=\"True\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("Indefinitely", result.Value);
    }

    [Fact]
    public void FromXml_Enum_ExtractsValue()
    {
        var param = MakeParam("enum", "RowPageLocation");
        var step = XElement.Parse("<Step><RowPageLocation value=\"Next\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("Next", result.Value);
    }

    [Fact]
    public void FromXml_Enum_WithHrEnumValues_MapsValue()
    {
        var param = MakeParam("enum", "RowPageLocation",
            hrEnumValues: new() { { "1", "First" }, { "2", "Last" } });
        var step = XElement.Parse("<Step><RowPageLocation value=\"1\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("First", result.Value);
    }

    [Fact]
    public void FromXml_Field_ExtractsTableAndName()
    {
        var param = MakeParam("field", "Field");
        var step = XElement.Parse("<Step><Field table=\"People\" name=\"FirstName\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("People::FirstName", result.Value);
    }

    [Fact]
    public void FromXml_Field_NoTable_ReturnsTextContent()
    {
        var param = MakeParam("field", "Field");
        var step = XElement.Parse("<Step><Field>$variable</Field></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("$variable", result.Value);
    }

    [Fact]
    public void FromXml_Script_ExtractsQuotedName()
    {
        var param = MakeParam("script", "Script");
        var step = XElement.Parse("<Step><Script id=\"1\" name=\"MyScript\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("\"MyScript\"", result.Value);
    }

    [Fact]
    public void FromXml_Layout_ExtractsQuotedName()
    {
        var param = MakeParam("layout", "Layout");
        var step = XElement.Parse("<Step><Layout id=\"1\" name=\"Detail\"/></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("\"Detail\"", result.Value);
    }

    [Fact]
    public void FromXml_MissingElement_ReturnsNullValue()
    {
        var param = MakeParam("calculation", "Calculation");
        var step = XElement.Parse("<Step></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Null(result.Value);
    }

    [Fact]
    public void FromXml_WithParentElement_FindsNested()
    {
        var param = MakeParam("calculation", "Calculation", parentElement: "Value");
        var step = XElement.Parse("<Step><Value><Calculation>42</Calculation></Value></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("42", result.Value);
    }

    [Fact]
    public void FromXml_WithWrapperElement_FindsNested()
    {
        var param = MakeParam("calculation", "Calculation", wrapperElement: "Value");
        var step = XElement.Parse("<Step><Value><Calculation>42</Calculation></Value></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("42", result.Value);
    }

    [Fact]
    public void FromXml_NamedCalc_ExtractsValue()
    {
        var param = MakeParam("namedCalc", "Calculation", wrapperElement: "Value");
        var step = XElement.Parse("<Step><Value><Calculation>$x + 1</Calculation></Value></Step>");

        var result = StepParamValue.FromXml(step, param);
        Assert.Equal("$x + 1", result.Value);
    }

    // --- ToXml building ---

    [Fact]
    public void ToXml_Calculation_BuildsCdata()
    {
        var param = MakeParam("calculation", "Calculation");
        var pv = new StepParamValue(param, "Get(AccountName)");

        var xml = pv.ToXml();
        Assert.NotNull(xml);
        Assert.Equal("Calculation", xml!.Name.LocalName);
        Assert.Contains("Get(AccountName)", xml.Value);
    }

    [Fact]
    public void ToXml_Text_BuildsElement()
    {
        var param = MakeParam("text", "Text");
        var pv = new StepParamValue(param, "hello");

        var xml = pv.ToXml();
        Assert.NotNull(xml);
        Assert.Equal("Text", xml!.Name.LocalName);
    }

    [Fact]
    public void ToXml_Text_NullValue_ReturnsNull()
    {
        var param = MakeParam("text", "Text");
        var pv = new StepParamValue(param);

        Assert.Null(pv.ToXml());
    }

    [Fact]
    public void ToXml_Boolean_On_SetsTrue()
    {
        var param = MakeParam("boolean", "Restore");
        var pv = new StepParamValue(param, "On");

        var xml = pv.ToXml();
        Assert.Equal("True", xml!.Attribute("state")!.Value);
    }

    [Fact]
    public void ToXml_Boolean_Off_SetsFalse()
    {
        var param = MakeParam("boolean", "Restore");
        var pv = new StepParamValue(param, "Off");

        var xml = pv.ToXml();
        Assert.Equal("False", xml!.Attribute("state")!.Value);
    }

    [Fact]
    public void ToXml_Boolean_Inverted_OnMeansFalse()
    {
        var param = MakeParam("boolean", "NoDialog", invertedHr: true);
        var pv = new StepParamValue(param, "On");

        var xml = pv.ToXml();
        Assert.Equal("False", xml!.Attribute("state")!.Value);
    }

    [Fact]
    public void ToXml_Boolean_Null_UsesDefault()
    {
        var param = MakeParam("boolean", "Restore", defaultValue: "True");
        var pv = new StepParamValue(param);

        var xml = pv.ToXml();
        Assert.Equal("True", xml!.Attribute("state")!.Value);
    }

    [Fact]
    public void ToXml_Boolean_WithHrEnumValues_ReverseMaps()
    {
        var param = MakeParam("boolean", "Pause",
            hrEnumValues: new() { { "True", "Indefinitely" }, { "False", "Off" } });
        var pv = new StepParamValue(param, "Indefinitely");

        var xml = pv.ToXml();
        Assert.Equal("True", xml!.Attribute("state")!.Value);
    }

    [Fact]
    public void ToXml_Enum_BuildsValueAttribute()
    {
        var param = MakeParam("enum", "RowPageLocation");
        var pv = new StepParamValue(param, "Next");

        var xml = pv.ToXml();
        Assert.Equal("Next", xml!.Attribute("value")!.Value);
    }

    [Fact]
    public void ToXml_Enum_NullValueAndDefault_ReturnsNull()
    {
        var param = MakeParam("enum", "RowPageLocation");
        var pv = new StepParamValue(param);

        Assert.Null(pv.ToXml());
    }

    [Fact]
    public void ToXml_Enum_WithHrEnumValues_ReverseMaps()
    {
        var param = MakeParam("enum", "RowPageLocation",
            hrEnumValues: new() { { "1", "First" }, { "2", "Last" } });
        var pv = new StepParamValue(param, "First");

        var xml = pv.ToXml();
        Assert.Equal("1", xml!.Attribute("value")!.Value);
    }

    [Fact]
    public void ToXml_Field_WithTableRef_BuildsAttributes()
    {
        var param = MakeParam("field", "Field");
        var pv = new StepParamValue(param, "People::FirstName");

        var xml = pv.ToXml();
        Assert.Equal("People", xml!.Attribute("table")!.Value);
        Assert.Equal("FirstName", xml.Attribute("name")!.Value);
    }

    [Fact]
    public void ToXml_Field_Variable_BuildsTextContent()
    {
        var param = MakeParam("field", "Field");
        var pv = new StepParamValue(param, "$myVar");

        var xml = pv.ToXml();
        Assert.Equal("$myVar", xml!.Value);
    }

    [Fact]
    public void ToXml_Field_Null_BuildsEmptyAttributes()
    {
        var param = MakeParam("field", "Field");
        var pv = new StepParamValue(param);

        var xml = pv.ToXml();
        Assert.Equal("", xml!.Attribute("table")!.Value);
        Assert.Equal("", xml.Attribute("name")!.Value);
    }

    [Fact]
    public void ToXml_Script_BuildsNamedRef()
    {
        var param = MakeParam("script", "Script");
        var pv = new StepParamValue(param, "\"MyScript\"");

        var xml = pv.ToXml();
        Assert.Equal("MyScript", xml!.Attribute("name")!.Value);
    }

    [Fact]
    public void ToXml_NamedCalc_WrapsInCalculation()
    {
        var param = MakeParam("namedCalc", "Calculation", wrapperElement: "Value");
        var pv = new StepParamValue(param, "$x + 1");

        var xml = pv.ToXml();
        Assert.Equal("Value", xml!.Name.LocalName);
        Assert.Contains("$x + 1", xml.Element("Calculation")!.Value);
    }

    // --- ToDisplayString ---

    [Fact]
    public void ToDisplayString_WithLabel_IncludesLabel()
    {
        var param = MakeParam("calculation", "Calculation", hrLabel: "Value");
        var pv = new StepParamValue(param, "42");

        Assert.Equal("Value: 42", pv.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_NoLabel_ReturnsValueOnly()
    {
        var param = MakeParam("calculation", "Calculation");
        var pv = new StepParamValue(param, "42");

        Assert.Equal("42", pv.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_NullValue_ReturnsNull()
    {
        var param = MakeParam("calculation", "Calculation", hrLabel: "Value");
        var pv = new StepParamValue(param);

        Assert.Null(pv.ToDisplayString());
    }

    [Fact]
    public void ToDisplayString_NamedCalcWithWrapper_UsesWrapperAsLabel()
    {
        var param = MakeParam("namedCalc", "Calculation", wrapperElement: "Value");
        var pv = new StepParamValue(param, "42");

        Assert.Equal("Value: 42", pv.ToDisplayString());
    }

    // --- Validate ---

    [Fact]
    public void Validate_NullValue_NoDiagnostics()
    {
        var param = MakeParam("field", "Field");
        var pv = new StepParamValue(param);

        Assert.Empty(pv.Validate(1));
    }

    [Fact]
    public void Validate_Field_ValidRef_NoDiagnostics()
    {
        var param = MakeParam("field", "Field");
        var pv = new StepParamValue(param, "People::Name");

        Assert.Empty(pv.Validate(1));
    }

    [Fact]
    public void Validate_Field_Variable_NoDiagnostics()
    {
        var param = MakeParam("field", "Field");
        var pv = new StepParamValue(param, "$myVar");

        Assert.Empty(pv.Validate(1));
    }

    [Fact]
    public void Validate_Field_InvalidRef_ProducesDiagnostic()
    {
        var param = MakeParam("field", "Field");
        var pv = new StepParamValue(param, "badref");

        var diagnostics = pv.Validate(1);
        Assert.Single(diagnostics);
        Assert.Contains("Table::Field", diagnostics[0].Message);
    }
}
