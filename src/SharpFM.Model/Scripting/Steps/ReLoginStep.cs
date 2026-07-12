using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ReLoginStep : ScriptStep<ReLoginStep>, IStepFactory
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
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Native, DisplayInverted = true },
            new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new NamedCalcChild("Password") { PocoProperty = "Password", HrLabel = "Password", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
    };
}
