using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class OpenDataFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 191;
    public const string XmlName = "Open Data File";

    public string Path { get; set; }
    public FieldRef? Target { get; set; }

    private OpenDataFileStep() : base(false) { Path = ""; }

    public OpenDataFileStep(string path = "", FieldRef? target = null, bool enabled = true)
        : base(enabled)
    {
        Path = path;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<OpenDataFileStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<OpenDataFileStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/open-data-file.html",
        // Canonical unconfigured form is empty: path and target are omitted when absent.
        Shape =
        [
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Required = true, Optional = true, DisplayEmptyAs = "" },
            new FieldChild() { PocoProperty = "Target", HrLabel = "Target", Optional = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
