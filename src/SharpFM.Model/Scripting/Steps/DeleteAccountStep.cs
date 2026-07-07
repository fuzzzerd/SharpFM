using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class DeleteAccountStep : ScriptStep, IStepFactory
{
    public const int XmlId = 135;
    public const string XmlName = "Delete Account";

    public Calculation AccountName { get; set; } = new("");

    private DeleteAccountStep() : base(false) { }

    public DeleteAccountStep(
        Calculation? accountName = null,
        bool enabled = true)
        : base(enabled)
    {
        AccountName = accountName ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<DeleteAccountStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<DeleteAccountStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/delete-account.html",
        // The AccountName wrapper is omitted by the unconfigured form (Optional).
        Shape =
        [
            new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
