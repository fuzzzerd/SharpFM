using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ChangePasswordStep : ScriptStep, IStepFactory
{
    public const int XmlId = 83;
    public const string XmlName = "Change Password";

    public Calculation? OldPassword { get; set; }
    public Calculation? Password { get; set; }
    public bool WithDialog { get; set; }

    /// <summary>
    /// XML-facing inverse of <see cref="WithDialog"/>: canonical
    /// <c>&lt;NoInteract state="…"/&gt;</c> suppresses the dialog, so it is the
    /// negation of the display-facing flag. The shape binds this so the raw
    /// state round-trips without re-applying the inversion.
    /// </summary>
    public bool NoInteract { get => !WithDialog; set => WithDialog = !value; }

    private ChangePasswordStep() : base(false) { }

    public ChangePasswordStep(
        Calculation? oldPassword = null,
        Calculation? password = null,
        bool withDialog = false,
        bool enabled = true)
        : base(enabled)
    {
        OldPassword = oldPassword;
        Password = password;
        WithDialog = withDialog;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ChangePasswordStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<ChangePasswordStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/change-password.html",
        // Canonical 083-ChangePassword: only NoInteract (always present, the
        // inverse of WithDialog). The <OldPassword>/<NewPassword> calculation
        // wrappers are omitted until configured, so both are Optional.
        Shape =
        [
            new NamedCalcChild("OldPassword") { PocoProperty = "OldPassword", HrLabel = "Old Password", Optional = true, DisplayEmptyAs = "" },
            new NamedCalcChild("NewPassword") { PocoProperty = "Password", HrLabel = "Password", Optional = true, DisplayEmptyAs = "" },
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteract", HrLabel = "With dialog", DisplayInverted = true },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
