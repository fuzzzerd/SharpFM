using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GetFileSizeStep : ScriptStep, IStepFactory
{
    public const int XmlId = 189;
    public const string XmlName = "Get File Size";

    public string Path { get; set; }
    public FieldRef? Target { get; set; }

    public GetFileSizeStep(string path = "", FieldRef? target = null, bool enabled = true)
        : base(enabled)
    {
        Path = path;
        Target = target;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("UniversalPathList", Path));
        if (Target is not null) step.Add(Target.ToXml("Field"));
        return step;
    }

    public override string ToDisplayLine() =>
        Target is null
            ? $"Get File Size [ {Path} ]"
            : $"Get File Size [ {Path} ; Target: {Target.ToDisplayString()} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var path = step.Element("UniversalPathList")?.Value ?? "";
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        return new GetFileSizeStep(path, target, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        string path = "";
        FieldRef? target = null;
        bool pathSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
            {
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            }
            else if (!pathSeen && !string.IsNullOrWhiteSpace(t))
            {
                path = t;
                pathSeen = true;
            }
        }
        return new GetFileSizeStep(path, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/get-file-size.html",
        Params =
        [
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text", Required = true },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Target" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
