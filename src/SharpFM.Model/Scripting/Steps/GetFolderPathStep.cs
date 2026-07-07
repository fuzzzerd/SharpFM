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

    private GetFolderPathStep() : base(false) { Name = ""; AllowFolderCreation = true; }

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

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<GetFolderPathStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<GetFolderPathStep>(enabled, hrParams, Metadata);

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
            new NamedTextChild("Name") { PocoProperty = "Name", Optional = true, DisplayEmptyAs = "" },
            new NamedCalcChild("DialogTitle") { PocoProperty = "Calculation", Optional = true, DisplayEmptyAs = "" },
            new NamedCalcChild("DefaultLocation") { PocoProperty = "Calculation2", Optional = true, DisplayEmptyAs = "" },
            new NamedCalcChild("Repetition") { PocoProperty = "Calculation3", Optional = true, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
