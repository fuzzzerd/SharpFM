using System.Xml.Linq;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Steps;
using Xunit;

namespace SharpFM.Tests.Scripting.Steps;

public class GetFolderPathStepTests
{
    private const string CanonicalXml = """<Step enable="True" id="181" name="Get Folder Path"><AllowFolderCreation state="True"/><Name>$example</Name><DialogTitle><Calculation><![CDATA[$x]]></Calculation></DialogTitle><DefaultLocation><Calculation><![CDATA[$x]]></Calculation></DefaultLocation><Repetition><Calculation><![CDATA[$x]]></Calculation></Repetition></Step>""";

    [Fact]
    public void RoundTrip_CanonicalXml_IsPreserved()
    {
        var source = XElement.Parse(CanonicalXml);
        var step = GetFolderPathStep.Metadata.FromXml!(source);
        Assert.True(XNode.DeepEquals(source, step.ToXml()));
    }

    [Fact]
    public void Registry_HasStep()
    {
        Assert.True(StepRegistry.ByName.TryGetValue("Get Folder Path", out var metadata));
        Assert.Equal(181, metadata!.Id);
    }
}
