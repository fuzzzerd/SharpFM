using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InstallPlugInFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 157;
    public const string XmlName = "Install Plug-In File";

    // Nullable so the unconfigured form (no Field) omits the optional <Field> node.
    public FieldRef? Target { get; set; }

    private InstallPlugInFileStep() : base(false) { }

    public InstallPlugInFileStep(
        FieldRef? target = null,
        bool enabled = true)
        : base(enabled)
    {
        Target = target;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Install Plug-In File [ " + (Target is null ? "" : Target.ToDisplayString()) + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InstallPlugInFileStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // The unconfigured form has no target field; only a non-empty token
        // carries one.
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        FieldRef? target = null;
        foreach (var tok in tokens) { if (!string.IsNullOrWhiteSpace(tok)) { target = FieldRef.FromDisplayToken(tok); break; } }
        return new InstallPlugInFileStep(target, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/install-plug-in-file.html",
        // The target Field is omitted by the unconfigured form (Optional).
        Shape =
        [
            new FieldChild("Field") { PocoProperty = "Target", Optional = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
