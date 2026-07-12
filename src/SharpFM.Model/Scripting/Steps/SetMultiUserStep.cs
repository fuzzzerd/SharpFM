using System;
using System.Collections.Generic;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetMultiUserStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;MultiUser value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class SetMultiUserStep : ScriptStep<SetMultiUserStep>, IStepFactory
{
    public const int XmlId = 84;
    public const string XmlName = "Set Multi-User";

    /// <summary>The enum XML value emitted on the <c>&lt;MultiUser&gt;</c> element.</summary>
    public string NetworkAccess { get; set; }

    private SetMultiUserStep() : base(false)
    {
        NetworkAccess = "True";
    }

    public SetMultiUserStep(string networkAccess = "True", bool enabled = true)
        : base(enabled)
    {
        NetworkAccess = networkAccess;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-multi-user.html",
        Shape =
        [
            new EnumValueChild("MultiUser") { PocoProperty = "NetworkAccess", HrLabel = "Network access", DefaultValue = "True", ValidValues = ["True", "OnHidden", "False"], DisplayValues = ["On", "On (Hidden)", "Off"] },
        ],
    };
}
