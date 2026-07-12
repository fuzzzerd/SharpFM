using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetDictionaryStep : ScriptStep<SetDictionaryStep>, IStepFactory
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
            new NamedTextChild("UniversalPathList") { PocoProperty = "UserDictionary", HrLabel = "User Dictionary", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
