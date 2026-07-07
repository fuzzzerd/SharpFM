using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetUseSystemFormatsStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Set state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class SetUseSystemFormatsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 94;
    public const string XmlName = "Set Use System Formats";

    /// <summary>The <c>Use system formats</c> flag on the step.</summary>
    public bool UseSystemFormats { get; set; }

    private SetUseSystemFormatsStep() : base(false) { }

    public SetUseSystemFormatsStep(bool usesystemformats = true, bool enabled = true)
        : base(enabled)
    {
        UseSystemFormats = usesystemformats;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        $"Set Use System Formats [ Use system formats: {(UseSystemFormats ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetUseSystemFormatsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        const string Prefix = "Use system formats:";
        if (token.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            token = token.Substring(Prefix.Length).Trim();
        var isOn = token.Equals("On", StringComparison.OrdinalIgnoreCase);
        return new SetUseSystemFormatsStep(isOn, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-use-system-formats.html",
        Shape =
        [
            new BoolStateChild("Set") { PocoProperty = "UseSystemFormats", HrLabel = "Use system formats" },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Set",
                XmlElement = "Set",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Use system formats",
                ValidValues = ["On", "Off"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
