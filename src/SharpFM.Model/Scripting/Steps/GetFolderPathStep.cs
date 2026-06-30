using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class GetFolderPathStep : ScriptStep, IStepFactory
{
    public const int XmlId = 181;
    public const string XmlName = "Get Folder Path";

    public bool AllowFolderCreation { get; set; }
    public string Name { get; set; }
    public Calculation? Calculation { get; set; }
    public Calculation? Calculation2 { get; set; }
    public Calculation? Calculation3 { get; set; }

    private GetFolderPathStep() : base(false) { Name = ""; }

    public GetFolderPathStep(
        bool allowFolderCreation = true,
        string name = "",
        Calculation? calculation = null,
        Calculation? calculation2 = null,
        Calculation? calculation3 = null,
        bool enabled = true)
        : base(enabled)
    {
        AllowFolderCreation = allowFolderCreation;
        Name = name;
        Calculation = calculation;
        Calculation2 = calculation2;
        Calculation3 = calculation3;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Get Folder Path [ " + "Allow Folder Creation: " + (AllowFolderCreation ? "On" : "Off") + " ; " + Name + " ; " + (Calculation?.Text ?? "") + " ; " + (Calculation2?.Text ?? "") + " ; " + (Calculation3?.Text ?? "") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<GetFolderPathStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool allowFolderCreation_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("Allow Folder Creation:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(22).Trim(); allowFolderCreation_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        string name_v = "";
        Calculation? calculation_v = null;
        foreach (var tok in tokens) { if (!(tok.StartsWith("Allow Folder Creation:", StringComparison.OrdinalIgnoreCase))) { calculation_v = new Calculation(tok); break; } }
        Calculation? calculation2_v = null;
        foreach (var tok in tokens) { if (!(tok.StartsWith("Allow Folder Creation:", StringComparison.OrdinalIgnoreCase))) { calculation2_v = new Calculation(tok); break; } }
        Calculation? calculation3_v = null;
        foreach (var tok in tokens) { if (!(tok.StartsWith("Allow Folder Creation:", StringComparison.OrdinalIgnoreCase))) { calculation3_v = new Calculation(tok); break; } }
        return new GetFolderPathStep(allowFolderCreation_v, name_v, calculation_v, calculation2_v, calculation3_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/get-directory.html",
        // Canonical form carries only AllowFolderCreation; name and the three
        // calc options (DialogTitle, DefaultLocation, Repetition) are omitted when blank.
        Shape =
        [
            new BoolStateChild("AllowFolderCreation") { PocoProperty = "AllowFolderCreation", HrLabel = "Allow Folder Creation" },
            new NamedTextChild("Name") { PocoProperty = "Name", Optional = true },
            new NamedCalcChild("DialogTitle") { PocoProperty = "Calculation", Optional = true },
            new NamedCalcChild("DefaultLocation") { PocoProperty = "Calculation2", Optional = true },
            new NamedCalcChild("Repetition") { PocoProperty = "Calculation3", Optional = true },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "AllowFolderCreation",
                XmlElement = "AllowFolderCreation",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Allow Folder Creation",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
            },
            new ParamMetadata
            {
                Name = "Name",
                XmlElement = "Name",
                Type = "text",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
