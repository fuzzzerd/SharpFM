using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

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
    public bool WithDialog { get; set; }
    public string DataSourceType { get; set; }
    public bool VerifySSLCertificates { get; set; }

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

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Option", new XAttribute("state", OpenFile ? "True" : "False")),
            new XElement("SkipIndexes", new XAttribute("state", SkipIndexes ? "True" : "False")),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")),
            new XElement("DataSourceType", new XAttribute("value", DataSourceType)),
            new XElement("VerifySSLCertificates", new XAttribute("state", VerifySSLCertificates ? "True" : "False")));

    public override string ToDisplayLine() =>
        "Convert File [ "
        + "Open File: " + (OpenFile ? "On" : "Off")
        + " ; Skip Indexes: " + (SkipIndexes ? "On" : "Off")
        + " ; With dialog: " + (WithDialog ? "On" : "Off")
        + " ; " + DataSourceType
        + " ; Verify SSL Certificates: " + (VerifySSLCertificates ? "On" : "Off")
        + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var option = step.Element("Option")?.Attribute("state")?.Value == "True";
        var skipIdx = step.Element("SkipIndexes")?.Attribute("state")?.Value == "True";
        var noInteract = step.Element("NoInteract")?.Attribute("state")?.Value == "True";
        var dataType = step.Element("DataSourceType")?.Attribute("value")?.Value ?? "File";
        var sslVerify = step.Element("VerifySSLCertificates")?.Attribute("state")?.Value == "True";
        return new ConvertFileStep(
            openFile: option,
            skipIndexes: skipIdx,
            withDialog: !noInteract,
            dataSourceType: dataType,
            verifySSLCertificates: sslVerify,
            enabled: enabled);
    }

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
