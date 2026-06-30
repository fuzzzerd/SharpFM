using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetDictionaryStep : ScriptStep, IStepFactory
{
    public const int XmlId = 209;
    public const string XmlName = "Set Dictionary";

    public string SpellingLanguage { get; set; }
    public string UserDictionary { get; set; }

    private SetDictionaryStep() : base(false) { SpellingLanguage = "US English"; UserDictionary = ""; }

    public SetDictionaryStep(
        string spellingLanguage = "US English",
        string userDictionary = "",
        bool enabled = true)
        : base(enabled)
    {
        SpellingLanguage = spellingLanguage;
        UserDictionary = userDictionary;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Set Dictionary [ " + "Spelling Language: " + SpellingLanguage + " ; " + "User Dictionary: " + UserDictionary + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetDictionaryStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string spellingLanguage_v = "US English";
        foreach (var tok in tokens) { if (tok.StartsWith("Spelling Language:", StringComparison.OrdinalIgnoreCase)) { spellingLanguage_v = tok.Substring(18).Trim(); break; } }
        string userDictionary_v = "";
        foreach (var tok in tokens) { if (tok.StartsWith("User Dictionary:", StringComparison.OrdinalIgnoreCase)) { userDictionary_v = tok.Substring(16).Trim(); break; } }
        return new SetDictionaryStep(spellingLanguage_v, userDictionary_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "spelling",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-dictionary.html",
        // Canonical form carries MainDictionary; the optional user-dictionary path is omitted when blank.
        Shape =
        [
            new EnumValueChild("MainDictionary") { PocoProperty = "SpellingLanguage", HrLabel = "Spelling Language", DefaultValue = "US English" },
            new NamedTextChild("UniversalPathList") { PocoProperty = "UserDictionary", HrLabel = "User Dictionary", Optional = true },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "MainDictionary",
                XmlElement = "MainDictionary",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Spelling Language",
                DefaultValue = "US English",
            },
            new ParamMetadata
            {
                Name = "UniversalPathList",
                XmlElement = "UniversalPathList",
                Type = "text",
                HrLabel = "User Dictionary",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
