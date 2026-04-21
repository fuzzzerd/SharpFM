using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

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

    public InsertAudioVideoStep(
        string path = "",
        string reference = "Embedded",
        bool enabled = true)
        : base(null, enabled)
    {
        Path = path;
        Reference = reference;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("UniversalPathList", new XAttribute("type", Reference), Path));

    public override string ToDisplayLine() =>
        $"Insert Audio/Video [ {Path} ; Reference: {Reference} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var path = step.Element("UniversalPathList")?.Value ?? "";
        var type = step.Element("UniversalPathList")?.Attribute("type")?.Value ?? "Embedded";
        return new InsertAudioVideoStep(path, type, enabled);
    }

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
