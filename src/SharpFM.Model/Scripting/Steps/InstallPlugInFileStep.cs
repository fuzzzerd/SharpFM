using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InstallPlugInFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 157;
    public const string XmlName = "Install Plug-In File";

    public FieldRef Target { get; set; }

    public InstallPlugInFileStep(
        FieldRef? target = null,
        bool enabled = true)
        : base(null, enabled)
    {
        Target = target ?? FieldRef.ForField("", 0, "");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            Target.ToXml("Field"));

    public override string ToDisplayLine() =>
        "Install Plug-In File [ " + Target.ToDisplayString() + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, "");
        return new InstallPlugInFileStep(target, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        FieldRef target = FieldRef.ForField("", 0, "");
        foreach (var tok in tokens) { if (true && !string.IsNullOrWhiteSpace(tok)) { target = FieldRef.FromDisplayToken(tok); break; } }
        return new InstallPlugInFileStep(target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/install-plug-in-file.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Field",
                XmlElement = "Field",
                Type = "field",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
