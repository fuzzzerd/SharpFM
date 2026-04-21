using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for DeleteFileStep: three &lt;Step&gt; attributes
/// plus a single &lt;UniversalPathList&gt; element carrying the target
/// file path as text. Display form uses the HR-label prefix
/// "Target file:" followed by the raw path string.
/// </summary>
public sealed class DeleteFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 197;
    public const string XmlName = "Delete File";

    public string TargetFile { get; set; }

    public DeleteFileStep(string targetFile = "", bool enabled = true) : base(null, enabled)
    {
        TargetFile = targetFile;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("UniversalPathList", TargetFile));

    public override string ToDisplayLine() =>
        $"Delete File [ Target file: {TargetFile} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var path = step.Element("UniversalPathList")?.Value ?? "";
        return new DeleteFileStep(path, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tok = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        const string Prefix = "Target file:";
        if (tok.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            tok = tok.Substring(Prefix.Length).Trim();
        return new DeleteFileStep(tok, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName, Id = XmlId, Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/delete-file.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "UniversalPathList", XmlElement = "UniversalPathList",
                Type = "text", HrLabel = "Target file", Required = true,
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
