using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ResetAccountPasswordStep : ScriptStep<ResetAccountPasswordStep>, IStepFactory
{
    public const int XmlId = 136;
    public const string XmlName = "Reset Account Password";

    public Calculation AccountName { get; set; } = new("");
    public Calculation Password { get; set; } = new("");
    public bool ExpirePassword { get; set; }

    /// <summary><c>&lt;ChgPwdOnNextLogin value&gt;</c> as the True/False string the shape emits. Bound by the shape.</summary>
    public string ExpirePasswordValue { get => ExpirePassword ? "True" : "False"; set => ExpirePassword = value == "True"; }

    private ResetAccountPasswordStep() : base(false) { }

    public ResetAccountPasswordStep(
        Calculation? accountName = null,
        Calculation? password = null,
        bool expirePassword = false,
        bool enabled = true)
        : base(enabled)
    {
        AccountName = accountName ?? new Calculation("");
        Password = password ?? new Calculation("");
        ExpirePassword = expirePassword;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "accounts",
        HelpUrl = "https://help.claris.com/en/pro-help/content/reset-account-password.html",
        // Canonical: AccountName and Password (omitted when empty, Optional) then
        // the always-present ChgPwdOnNextLogin value flag.
        Shape =
        [
            new NamedCalcChild("AccountName") { PocoProperty = "AccountName", HrLabel = "Account Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new NamedCalcChild("Password") { PocoProperty = "Password", HrLabel = "Password", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            // No DefaultValue: the flag must stay visible in the display line even
            // when "False" (the POCO default matches the absent-element fallback).
            new EnumValueChild("ChgPwdOnNextLogin") { PocoProperty = "ExpirePasswordValue", HrLabel = "Expire password", ValidValues = ["True", "False"], DisplayValues = ["On", "Off"], Display = DisplayMode.Augmented },
        ],
    };
}
