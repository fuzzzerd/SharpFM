using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Set AI Call Logging [ On/Off ; Filename: calc ; Verbose: On/Off ; Truncate Messages: On/Off ].
/// The Filename, Verbose, and Truncate options live inside an
/// <c>&lt;LLMDebugLog&gt;</c> parent element. Each flag is a
/// "flagElement" — presence = On, absence = Off.
/// </summary>
public sealed class SetAICallLoggingStep : ScriptStep, IStepFactory
{
    public const int XmlId = 217;
    public const string XmlName = "Set AI Call Logging";

    public bool Logging { get; set; }
    public Calculation? FileName { get; set; }
    public bool Verbose { get; set; }
    public bool TruncateMessages { get; set; }

    private SetAICallLoggingStep() : base(false) { }

    public SetAICallLoggingStep(
        bool logging = false,
        Calculation? fileName = null,
        bool verbose = false,
        bool truncateMessages = false,
        bool enabled = true)
        : base(enabled)
    {
        Logging = logging;
        FileName = fileName;
        Verbose = verbose;
        TruncateMessages = truncateMessages;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string> { Logging ? "On" : "Off" };
        if (FileName is not null) parts.Add($"Filename: {FileName.Text}");
        parts.Add($"Verbose: {(Verbose ? "On" : "Off")}");
        parts.Add($"Truncate Messages: {(TruncateMessages ? "On" : "Off")}");
        return $"Set AI Call Logging [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetAICallLoggingStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool logging = false;
        Calculation? fileName = null;
        bool verbose = false;
        bool truncate = false;
        bool loggingSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Filename:", StringComparison.OrdinalIgnoreCase))
                fileName = new Calculation(t.Substring(9).Trim());
            else if (t.StartsWith("Verbose:", StringComparison.OrdinalIgnoreCase))
                verbose = t.Substring(8).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Truncate Messages:", StringComparison.OrdinalIgnoreCase))
                truncate = t.Substring(18).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (!loggingSeen)
            {
                logging = t.Equals("On", StringComparison.OrdinalIgnoreCase);
                loggingSeen = true;
            }
        }
        return new SetAICallLoggingStep(logging, fileName, verbose, truncate, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        Shape =
        [
            new BoolStateChild("Set") { PocoProperty = "Logging", HrLabel = "Logging", Required = true },
            // The <LLMDebugLog> wrapper is always emitted, empty when no
            // option inside it is configured.
            new WrapperChild("LLMDebugLog",
            [
                new NamedCalcChild("FileName") { Optional = true, HrLabel = "Filename" },
                new FlagChild("VerboseMode") { PocoProperty = "Verbose", HrLabel = "Verbose" },
                new FlagChild("TruncateEmbeddingVectorsMode") { PocoProperty = "TruncateMessages", HrLabel = "Truncate Messages" },
            ]),
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Set",
                XmlElement = "Set",
                XmlAttr = "state",
                Type = "boolean",
                HrLabel = "Logging",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
                Required = true,
            },
            new ParamMetadata { Name = "FileName", XmlElement = "Calculation", Type = "namedCalc", HrLabel = "Filename" },
            new ParamMetadata { Name = "VerboseMode", XmlElement = "VerboseMode", Type = "flagElement", HrLabel = "Verbose" },
            new ParamMetadata { Name = "TruncateEmbeddingVectorsMode", XmlElement = "TruncateEmbeddingVectorsMode", Type = "flagElement", HrLabel = "Truncate Messages" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
