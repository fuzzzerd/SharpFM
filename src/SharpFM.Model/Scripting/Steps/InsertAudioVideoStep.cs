using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for InsertAudioVideoStep: three &lt;Step&gt;
/// attributes plus a single &lt;UniversalPathList type="..."&gt; element
/// that carries BOTH the file path as text content AND a "type"
/// attribute marking the payload as embedded or a reference.
/// </summary>
public sealed class InsertAudioVideoStep : ScriptStep, IStepFactory
{
    public const int XmlId = 159;
    public const string XmlName = "Insert Audio/Video";

    public string Path { get; set; }
    public string Reference { get; set; }

    private InsertAudioVideoStep() : this("") { }

    public InsertAudioVideoStep(
        string path = "",
        string reference = "Embedded",
        bool enabled = true)
        : base(enabled)
    {
        Path = path;
        Reference = reference;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Insert Audio/Video [ {Path} ; Reference: {Reference} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertAudioVideoStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string reference = "Embedded";
        foreach (var tok in tokens)
        {
            if (tok.StartsWith("Reference:", StringComparison.OrdinalIgnoreCase))
            {
                reference = tok.Substring("Reference:".Length).Trim();
                break;
            }
        }
        var path = tokens.FirstOrDefault(t =>
            !t.StartsWith("Reference:", StringComparison.OrdinalIgnoreCase)) ?? "";
        return new InsertAudioVideoStep(path, reference, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName, Id = XmlId, Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-audio-video.html",
        Shape =
        [
            new NamedTextChild("UniversalPathList")
            {
                PocoProperty = "Path",
                Attr = "type",
                AttrProperty = "Reference",
                AttrDefault = "Embedded",
                HrLabel = "Reference",
                ValidValues = ["Embedded", "Reference"],
            },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "UniversalPathList", XmlElement = "UniversalPathList",
                Type = "text",
            },
            new ParamMetadata
            {
                Name = "Reference", XmlElement = "UniversalPathList", Type = "enum",
                XmlAttr = "type", HrLabel = "Reference",
                ValidValues = ["Embedded", "Reference"], DefaultValue = "Embedded",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
