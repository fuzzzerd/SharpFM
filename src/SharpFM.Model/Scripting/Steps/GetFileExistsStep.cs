using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GetFileExistsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 188;
    public const string XmlName = "Get File Exists";

    public string Path { get; set; }
    public FieldRef? Target { get; set; }

    private GetFileExistsStep() : base(false) { Path = ""; }

    public GetFileExistsStep(string path = "", FieldRef? target = null, bool enabled = true)
        : base(enabled)
    {
        Path = path;
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        Target is null
            ? $"Get File Exists [ {Path} ]"
            : $"Get File Exists [ {Path} ; Target: {Target.ToDisplayString()} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<GetFileExistsStep>(step, Metadata);

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
        return new GetFileExistsStep(path, target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/get-file-exists.html",
        // Canonical unconfigured form is empty: path and target are omitted when absent.
        Shape =
        [
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Required = true, Optional = true },
            new FieldChild() { PocoProperty = "Target", HrLabel = "Target", Optional = true },
        ],
        Params =
        [
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text", Required = true },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", HrLabel = "Target" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
