using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetDictionaryStep : ScriptStep, IStepFactory
{
    public const int XmlId = 209;
    public const string XmlName = "Set Dictionary";

    public string SpellingLanguage { get; set; }
    public string UserDictionary { get; set; }

    public SetDictionaryStep(
        string spellingLanguage = "US English",
        string userDictionary = "",
        bool enabled = true)
        : base(null, enabled)
    {
        SpellingLanguage = spellingLanguage;
        UserDictionary = userDictionary;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("MainDictionary", new XAttribute("value", SpellingLanguage)),
            new XElement("UniversalPathList", UserDictionary));

    public override string ToDisplayLine() =>
        "Set Dictionary [ " + "Spelling Language: " + SpellingLanguage + " ; " + "User Dictionary: " + UserDictionary + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var spellingLanguage_v = step.Element("MainDictionary")?.Attribute("value")?.Value ?? "US English";
        var userDictionary_v = step.Element("UniversalPathList")?.Value ?? "";
        return new SetDictionaryStep(spellingLanguage_v, userDictionary_v, enabled);
    }

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
