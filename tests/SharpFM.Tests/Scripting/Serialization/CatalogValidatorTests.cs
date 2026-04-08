using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Serialization;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

public class CatalogValidatorTests
{
    private static XElement Parse(string xml) => XElement.Parse(xml);

    [Fact]
    public void Validate_ValidStep_ReturnsNoDiagnostics()
    {
        var el = Parse(
            "<Step enable=\"True\" id=\"68\" name=\"If\">"
            + "<Calculation><![CDATA[$x > 0]]></Calculation></Step>");
        var def = StepCatalogLoader.ByName["If"];

        var diagnostics = CatalogValidator.Validate(el, def, lineIndex: 0);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validate_NoParams_ReturnsNoDiagnostics()
    {
        var el = Parse("<Step enable=\"True\" id=\"69\" name=\"Else\"/>");
        var def = StepCatalogLoader.ByName["Else"];

        var diagnostics = CatalogValidator.Validate(el, def, lineIndex: 0);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validate_FieldParam_BadValue_EmitsFieldOrVariableWarning()
    {
        // Set Field's Field param is of type "field" — a bare identifier
        // without :: or $ prefix should trip the field-or-variable diagnostic.
        var el = Parse(
            "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Calculation><![CDATA[1]]></Calculation>"
            + "<Field table=\"\" id=\"0\" name=\"bareFieldName\"/></Step>");
        var def = StepCatalogLoader.ByName["Set Field"];

        var diagnostics = CatalogValidator.Validate(el, def, lineIndex: 3);

        Assert.Contains(diagnostics, d =>
            d.Message.Contains("Expected field reference")
            && d.Line == 3);
    }

    [Fact]
    public void Validate_FieldParam_TableQualified_PassesValidation()
    {
        var el = Parse(
            "<Step enable=\"True\" id=\"76\" name=\"Set Field\">"
            + "<Calculation><![CDATA[1]]></Calculation>"
            + "<Field table=\"People\" id=\"1\" name=\"FirstName\"/></Step>");
        var def = StepCatalogLoader.ByName["Set Field"];

        var diagnostics = CatalogValidator.Validate(el, def, lineIndex: 0);

        Assert.DoesNotContain(diagnostics, d => d.Message.Contains("Expected field reference"));
    }

    [Fact]
    public void Validate_MissingOptionalParam_IsSilent()
    {
        var el = Parse("<Step enable=\"True\" id=\"6\" name=\"Go to Layout\">"
            + "<LayoutDestination value=\"OriginalLayout\"/></Step>");
        var def = StepCatalogLoader.ByName["Go to Layout"];

        var diagnostics = CatalogValidator.Validate(el, def, lineIndex: 0);

        // OriginalLayout has no Layout or Animation element — both optional.
        Assert.Empty(diagnostics);
    }
}
