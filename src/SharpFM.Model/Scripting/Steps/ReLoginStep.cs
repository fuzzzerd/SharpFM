using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ReLoginStep : ScriptStep, IStepFactory
{
    public const int XmlId = 138;
    public const string XmlName = "Re-Login";

    public bool WithDialog { get; set; }
    public Calculation AccountName { get; set; } = new("");
    public Calculation Password { get; set; } = new("");

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private ReLoginStep() : base(false) { }

    public ReLoginStep(
        bool withDialog = true,
        Calculation? accountName = null,
        Calculation? password = null,
        bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        AccountName = accountName ?? new Calculation("");
        Password = password ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Re-Login [ " + "With dialog: " + (WithDialog ? "On" : "Off") + " ; " + "Account Name: " + AccountName.Text + " ; " + "Password: " + Password.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ReLoginStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        bool withDialog_v = true;
        foreach (var tok in tokens) { if (tok.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(12).Trim(); withDialog_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        Calculation? accountName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Account Name:", StringComparison.OrdinalIgnoreCase)) { accountName_v = new Calculation(tok.Substring(13).Trim()); break; } }
        Calculation? password_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Password:", StringComparison.OrdinalIgnoreCase)) { password_v = new Calculation(tok.Substring(9).Trim()); break; } }
        return new ReLoginStep(withDialog_v, accountName_v, password_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/re-login.html",
        // NoInteract (inverts WithDialog) then AccountName/Password, which the
        // unconfigured form omits (Optional).
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Native },
            new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Native },
            new NamedCalcChild("Password") { PocoProperty = "Password", HrLabel = "Password", Optional = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
