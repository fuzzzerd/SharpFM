using SharpFM.Model.ClipTypes;
using SharpFM.Model.Parsing;

namespace SharpFM.Tests.ClipTypes;

public class TableClipStrategyTests
{
    [Fact]
    public void TableAndField_HaveDistinctIdentities()
    {
        Assert.Equal("Mac-XMTB", TableClipStrategy.Table.FormatId);
        Assert.Equal("Mac-XMFD", TableClipStrategy.Field.FormatId);
    }

    [Fact]
    public void Parse_ValidTable_ReturnsSuccess()
    {
        const string xml = """
            <fmxmlsnippet type="FMObjectList">
                <BaseTable name="People" id="1">
                    <Field id="1" name="ID" dataType="Number" fieldType="Normal"/>
                    <Field id="2" name="Name" dataType="Text" fieldType="Normal"/>
                </BaseTable>
            </fmxmlsnippet>
            """;

        var result = TableClipStrategy.Table.Parse(xml);

        var success = Assert.IsType<ParseSuccess>(result);
        var model = Assert.IsType<TableClipModel>(success.Model);
        Assert.Equal("People", model.Table.Name);
        Assert.Equal(2, model.Table.Fields.Count);
    }

    [Fact]
    public void Parse_MalformedXml_ReturnsFailure()
    {
        var result = TableClipStrategy.Table.Parse("<not closed");
        Assert.IsType<ParseFailure>(result);
    }

    [Fact]
    public void Parse_WrongRoot_ReturnsUnsupportedClipType()
    {
        var result = TableClipStrategy.Table.Parse("<wrongroot/>");
        var failure = Assert.IsType<ParseFailure>(result);
        Assert.Equal(ParseDiagnosticKind.UnsupportedClipType, failure.Report.Diagnostics[0].Kind);
    }

    [Fact]
    public void Table_DefaultXml_IncludesBaseTableWrapper()
    {
        var seed = TableClipStrategy.Table.DefaultXml("My Table");
        Assert.Contains("<BaseTable", seed);
        Assert.Contains("name=\"My Table\"", seed);
    }

    [Fact]
    public void Field_DefaultXml_HasNoBaseTableWrapper()
    {
        var seed = TableClipStrategy.Field.DefaultXml("anything");
        Assert.DoesNotContain("<BaseTable", seed);
    }

    [Fact]
    public void DefaultXml_ProducesParseableSnippet()
    {
        var seed = TableClipStrategy.Table.DefaultXml("X");
        var result = TableClipStrategy.Table.Parse(seed);
        Assert.IsType<ParseSuccess>(result);
    }
}
