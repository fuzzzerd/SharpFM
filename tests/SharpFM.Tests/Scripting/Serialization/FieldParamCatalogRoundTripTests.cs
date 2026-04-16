using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Serialization;
using Xunit;

namespace SharpFM.Tests.Scripting.Serialization;

/// <summary>
/// Coverage for the field-param refactor on the catalog (RawStep) side:
/// <c>CatalogParamExtractor.ExtractField</c>, <c>CatalogXmlBuilder.BuildFieldXml</c>,
/// and <c>CatalogValidator</c>'s tolerance for the lossless <c>(#id)</c>
/// suffix. The 47-ish catalog-path field params rely on these helpers
/// for id preservation across display-text editing.
/// </summary>
public class FieldParamCatalogRoundTripTests
{
    private static XElement MakeStep(string xml) => XElement.Parse(xml);

    // "Check Selection" (id=18) is a catalog-known, non-POCO step with
    // SelectAll (boolean) + Field params. Both elements are present so the
    // extract-then-rebuild round-trip has tokens for each catalog slot.
    private const string CheckSelectionXml =
        "<Step enable=\"True\" id=\"18\" name=\"Check Selection\">"
        + "<SelectAll state=\"True\"></SelectAll>"
        + "<Field table=\"People\" id=\"7\" name=\"Email\"/></Step>";

    [Fact]
    public void Extract_FieldParam_IncludesIdSuffix()
    {
        var el = MakeStep(CheckSelectionXml);
        var def = StepCatalogLoader.ByName["Check Selection"];

        // The field param is the first (and only) param.
        var fieldParam = def.Params.First(p => p.Type == "field" || p.Type == "fieldOrVariable");
        var extracted = CatalogParamExtractor.Extract(el, fieldParam);

        Assert.Equal("People::Email (#7)", extracted);
    }

    [Fact]
    public void Build_FieldParam_ParsesIdSuffixFromDisplayToken()
    {
        var def = StepCatalogLoader.ByName["Check Selection"];

        // Check Selection has two params — SelectAll (boolean, label "Select")
        // and Field (no label). Pass both tokens so positional matching
        // assigns the field token to the Field slot.
        var step = CatalogXmlBuilder.BuildStep(def, enabled: true,
            hrParams: new[] { "On", "People::Email (#7)" });

        var field = step.Element("Field");
        Assert.NotNull(field);
        Assert.Equal("People", field!.Attribute("table")!.Value);
        Assert.Equal("7", field.Attribute("id")!.Value);
        Assert.Equal("Email", field.Attribute("name")!.Value);
    }

    [Fact]
    public void Build_FieldParam_NoIdSuffix_YieldsIdZero()
    {
        // Back-compat: tokens without (#id) still work (id=0 sentinel).
        var def = StepCatalogLoader.ByName["Check Selection"];
        var step = CatalogXmlBuilder.BuildStep(def, enabled: true,
            hrParams: new[] { "On", "People::Email" });

        var field = step.Element("Field");
        Assert.Equal("People", field!.Attribute("table")!.Value);
        Assert.Equal("0", field.Attribute("id")!.Value);
    }

    [Fact]
    public void FullRoundTrip_FieldParam_PreservesIdAttribute()
    {
        // Extract BOTH params from source display-side, rebuild from
        // those tokens, verify Field attributes survive byte-for-byte.
        var source = MakeStep(CheckSelectionXml);
        var def = StepCatalogLoader.ByName["Check Selection"];

        var tokens = def.Params
            .Select(p =>
            {
                var extracted = CatalogParamExtractor.Extract(source, p);
                // Label-prefixed tokens mimic the display format
                // MatchDisplayParams expects for labelled params.
                return p.HrLabel != null && extracted != null
                    ? $"{p.HrLabel}: {extracted}"
                    : extracted;
            })
            .Where(t => t != null)
            .Select(t => t!)
            .ToArray();

        var rebuilt = CatalogXmlBuilder.BuildStep(def, enabled: true, hrParams: tokens);
        var originalField = source.Element("Field")!;
        var rebuiltField = rebuilt.Element("Field")!;

        Assert.Equal(originalField.Attribute("table")!.Value, rebuiltField.Attribute("table")!.Value);
        Assert.Equal(originalField.Attribute("id")!.Value, rebuiltField.Attribute("id")!.Value);
        Assert.Equal(originalField.Attribute("name")!.Value, rebuiltField.Attribute("name")!.Value);
    }

    [Fact]
    public void Validate_FieldParam_WithIdSuffix_ProducesNoWarning()
    {
        // The validator's field-shape check must tolerate the (#id) suffix.
        // Without the refactor, "People::Email (#7)" failed the shape check
        // because it didn't contain literal "::" on its own (after the suffix).
        var el = MakeStep(CheckSelectionXml);
        var def = StepCatalogLoader.ByName["Check Selection"];

        var diagnostics = CatalogValidator.Validate(el, def, lineIndex: 0);

        Assert.DoesNotContain(diagnostics, d =>
            d.Message.Contains("Expected field reference"));
    }
}
