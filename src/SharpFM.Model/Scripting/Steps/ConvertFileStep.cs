using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ConvertFileStep: the step's XML state is the
/// three &lt;Step&gt; attributes plus five child elements — four
/// booleans (three labeled, one inverted) and an unlabeled positional
/// enum (<c>DataSourceType</c>) carrying "File" or "XMLSource".
/// </summary>
public sealed class ConvertFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 139;
    public const string XmlName = "Convert File";

    public bool OpenFile { get; set; }
    public bool SkipIndexes { get; set; }

    /// <summary>Raw <c>&lt;NoInteract&gt;</c> state. <see cref="WithDialog"/> is its inverse.</summary>
    public bool NoInteract { get; set; }

    /// <summary>UI-facing "with dialog" flag — the inverse of the wire <c>NoInteract</c> state.</summary>
    public bool WithDialog { get => !NoInteract; set => NoInteract = !value; }

    public string DataSourceType { get; set; } = "File";
    public bool VerifySSLCertificates { get; set; }

    private ConvertFileStep() : base(false) { }

    public ConvertFileStep(
        bool openFile = false,
        bool skipIndexes = false,
        bool withDialog = true,
        string dataSourceType = "File",
        bool verifySSLCertificates = false,
        bool enabled = true)
        : base(enabled)
    {
        OpenFile = openFile;
        SkipIndexes = skipIndexes;
        WithDialog = withDialog;
        DataSourceType = dataSourceType;
        VerifySSLCertificates = verifySSLCertificates;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Convert File [ "
        + "Open File: " + (OpenFile ? "On" : "Off")
        + " ; Skip Indexes: " + (SkipIndexes ? "On" : "Off")
        + " ; With dialog: " + (WithDialog ? "On" : "Off")
        + " ; " + DataSourceType
        + " ; Verify SSL Certificates: " + (VerifySSLCertificates ? "On" : "Off")
        + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ConvertFileStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        var openFile = ParseOn(tokens, "Open File:", defaultValue: false);
        var skipIdx = ParseOn(tokens, "Skip Indexes:", defaultValue: false);
        var withDialog = ParseOn(tokens, "With dialog:", defaultValue: true);
        var sslVerify = ParseOn(tokens, "Verify SSL Certificates:", defaultValue: false);
        // DataSourceType is the unlabeled positional — the token that
        // doesn't start with any known label prefix.
        var dataType = tokens.FirstOrDefault(t =>
            !t.StartsWith("Open File:", StringComparison.OrdinalIgnoreCase) &&
            !t.StartsWith("Skip Indexes:", StringComparison.OrdinalIgnoreCase) &&
            !t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase) &&
            !t.StartsWith("Verify SSL Certificates:", StringComparison.OrdinalIgnoreCase))
            ?? "File";
        return new ConvertFileStep(openFile, skipIdx, withDialog, dataType, sslVerify, enabled);
    }

    private static bool ParseOn(string[] tokens, string prefix, bool defaultValue)
    {
        foreach (var tok in tokens)
        {
            if (tok.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var v = tok.Substring(prefix.Length).Trim();
                return v.Equals("On", StringComparison.OrdinalIgnoreCase);
            }
        }
        return defaultValue;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/convert-file.html",
        // Canonical order: NoInteract, Option, SkipIndexes, VerifySSLCertificates,
        // then the optional DataSourceType (omitted for the default File source).
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteract", HrLabel = "With dialog", Display = DisplayMode.Augmented },
            new BoolStateChild("Option") { PocoProperty = "OpenFile", HrLabel = "Open File", Display = DisplayMode.Augmented },
            new BoolStateChild("SkipIndexes") { PocoProperty = "SkipIndexes", HrLabel = "Skip Indexes", Display = DisplayMode.Augmented },
            new BoolStateChild("VerifySSLCertificates") { PocoProperty = "VerifySSLCertificates", HrLabel = "Verify SSL Certificates", Display = DisplayMode.Augmented },
            new EnumValueChild("DataSourceType") { PocoProperty = "DataSourceType", Optional = true, Display = DisplayMode.Native },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Option", XmlElement = "Option", Type = "boolean",
                XmlAttr = "state", HrLabel = "Open File",
                ValidValues = ["On", "Off"], DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "SkipIndexes", XmlElement = "SkipIndexes", Type = "boolean",
                XmlAttr = "state", HrLabel = "Skip Indexes",
                ValidValues = ["On", "Off"], DefaultValue = "False",
            },
            new ParamMetadata
            {
                Name = "NoInteract", XmlElement = "NoInteract", Type = "boolean",
                XmlAttr = "state", HrLabel = "With dialog",
                // Inverted: XML state=True means HR "With dialog: Off".
                ValidValues = ["On", "Off"], DefaultValue = "True",
            },
            new ParamMetadata
            {
                Name = "DataSourceType", XmlElement = "DataSourceType", Type = "enum",
                XmlAttr = "value",
                ValidValues = ["File", "XMLSource"], DefaultValue = "File",
            },
            new ParamMetadata
            {
                Name = "VerifySSLCertificates", XmlElement = "VerifySSLCertificates", Type = "boolean",
                XmlAttr = "state", HrLabel = "Verify SSL Certificates",
                ValidValues = ["On", "Off"], DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
