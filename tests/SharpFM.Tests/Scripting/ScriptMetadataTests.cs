using System.Xml.Linq;
using SharpFM.Model.Scripting;
using Xunit;

namespace SharpFM.Tests.Scripting;

/// <summary>
/// Tests for the Mac-XMSC ("Script") vs Mac-XMSS ("ScriptSteps") wrapper
/// round-trip. FM Pro distinguishes the two formats by the presence of
/// a <c>&lt;Script&gt;</c> envelope around the step list; a Mac-XMSC clip
/// without the wrapper is rejected on paste. <see cref="FmScript.Metadata"/>
/// captures the envelope's attributes and drives the decision to emit it.
/// </summary>
public class ScriptMetadataTests
{
    private const string XmscWithWrapperXml = @"<fmxmlsnippet type=""FMObjectList"">
        <Script includeInMenu=""True"" runFullAccess=""False"" id=""9"" name=""Control-Flow-Script-Step-Examples"">
            <Step enable=""True"" id=""68"" name=""If"">
                <Calculation><![CDATA[$x > 0]]></Calculation>
            </Step>
            <Step enable=""True"" id=""70"" name=""End If""></Step>
        </Script>
    </fmxmlsnippet>";

    private const string XmssNoWrapperXml = @"<fmxmlsnippet type=""FMObjectList"">
        <Step enable=""True"" id=""68"" name=""If"">
            <Calculation><![CDATA[$x > 0]]></Calculation>
        </Step>
        <Step enable=""True"" id=""70"" name=""End If""></Step>
    </fmxmlsnippet>";

    [Fact]
    public void FromXml_ScriptWrapper_PopulatesMetadata()
    {
        var script = FmScript.FromXml(XmscWithWrapperXml);

        Assert.NotNull(script.Metadata);
        Assert.Equal(9, script.Metadata!.Id);
        Assert.Equal("Control-Flow-Script-Step-Examples", script.Metadata.Name);
        Assert.True(script.Metadata.IncludeInMenu);
        Assert.False(script.Metadata.RunFullAccess);
        Assert.Equal(2, script.Steps.Count);
    }

    [Fact]
    public void FromXml_NoWrapper_MetadataIsNull()
    {
        var script = FmScript.FromXml(XmssNoWrapperXml);

        Assert.Null(script.Metadata);
        Assert.Equal(2, script.Steps.Count);
    }

    [Fact]
    public void ToXml_WithMetadata_EmitsScriptWrapper()
    {
        var script = FmScript.FromXml(XmscWithWrapperXml);
        var xml = XDocument.Parse(script.ToXml());

        var wrapper = xml.Root!.Element("Script");
        Assert.NotNull(wrapper);
        Assert.Equal("9", wrapper!.Attribute("id")!.Value);
        Assert.Equal("Control-Flow-Script-Step-Examples", wrapper.Attribute("name")!.Value);
        Assert.Equal("True", wrapper.Attribute("includeInMenu")!.Value);
        Assert.Equal("False", wrapper.Attribute("runFullAccess")!.Value);

        // Steps must be inside the wrapper, not directly under fmxmlsnippet.
        Assert.Empty(xml.Root.Elements("Step"));
        Assert.Equal(2, wrapper.Elements("Step").Count());
    }

    [Fact]
    public void ToXml_NoMetadata_OmitsScriptWrapper()
    {
        var script = FmScript.FromXml(XmssNoWrapperXml);
        var xml = XDocument.Parse(script.ToXml());

        Assert.Null(xml.Root!.Element("Script"));
        Assert.Equal(2, xml.Root.Elements("Step").Count());
    }

    [Fact]
    public void RoundTrip_Xmsc_PreservesWrapperAttrsAndSteps()
    {
        // The core regression test: paste-from-FM → paste-to-FM for a
        // Mac-XMSC clip must preserve the <Script> envelope byte-compat
        // with what FM Pro expects.
        var script1 = FmScript.FromXml(XmscWithWrapperXml);
        var emitted = script1.ToXml();
        var script2 = FmScript.FromXml(emitted);

        Assert.Equal(script1.Metadata, script2.Metadata);
        Assert.Equal(script1.Steps.Count, script2.Steps.Count);
    }

    [Fact]
    public void PromoteToScript_NullMetadata_CanBeDefaultedAndEmitsWrapper()
    {
        // Flow: user has a XMSS clip and chooses "Copy as Script".
        // MainWindowViewModel supplies a default ScriptMetadata; ToXml
        // must then emit the wrapper even though the clip started bare.
        var script = FmScript.FromXml(XmssNoWrapperXml);
        Assert.Null(script.Metadata);

        script.Metadata = ScriptMetadata.Default("promoted-script");
        var xml = XDocument.Parse(script.ToXml());

        var wrapper = xml.Root!.Element("Script");
        Assert.NotNull(wrapper);
        Assert.Equal("promoted-script", wrapper!.Attribute("name")!.Value);
    }

    [Fact]
    public void DemoteToScriptSteps_ClearingMetadata_OmitsWrapperOnNextEmit()
    {
        // Flow: user has a XMSC clip and chooses "Copy as Script Steps".
        // Setting Metadata=null before ToXml must produce the bare shape.
        var script = FmScript.FromXml(XmscWithWrapperXml);
        Assert.NotNull(script.Metadata);

        script.Metadata = null;
        var xml = XDocument.Parse(script.ToXml());

        Assert.Null(xml.Root!.Element("Script"));
        Assert.Equal(2, xml.Root.Elements("Step").Count());
    }
}
