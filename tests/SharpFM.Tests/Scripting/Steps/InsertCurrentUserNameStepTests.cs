using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class InsertCurrentUserNameStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="60" name="Insert Current User Name"><SelectAll state="False"/><Field table="Invoices" id="12" name="Status"/></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = InsertCurrentUserNameStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Insert Current User Name", out var metadata));
        Assert.Equal(60, metadata!.Id);
    }
}
