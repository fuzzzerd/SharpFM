using System;
using System.Collections.Generic;
using System.Linq;
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

    public override string ToDisplayLine() =>
        "Change Password [ " + "Old Password: " + (OldPassword?.Text ?? "") + " ; " + "Password: " + (Password?.Text ?? "") + " ; " + "With dialog: " + (WithDialog ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ChangePasswordStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? oldPassword_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Old Password:", StringComparison.OrdinalIgnoreCase)) { oldPassword_v = new Calculation(tok.Substring(13).Trim()); break; } }
        Calculation? password_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Password:", StringComparison.OrdinalIgnoreCase)) { password_v = new Calculation(tok.Substring(9).Trim()); break; } }
        bool withDialog_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        return new ChangePasswordStep(oldPassword_v, password_v, withDialog_v, enabled);
    }

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
            new NamedCalcChild("OldPassword") { PocoProperty = "OldPassword", HrLabel = "Old Password", Optional = true },
            new NamedCalcChild("NewPassword") { PocoProperty = "Password", HrLabel = "Password", Optional = true },
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteract", HrLabel = "With dialog" },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Old Password",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Password",
            },
            new ParamMetadata
            {
                Name = "NoInteract",
                XmlElement = "NoInteract",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "With dialog",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
