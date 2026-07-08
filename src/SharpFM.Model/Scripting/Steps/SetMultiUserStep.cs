using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetMultiUserStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;MultiUser value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class SetMultiUserStep : ScriptStep, IStepFactory
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

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetMultiUserStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<SetMultiUserStep>(enabled, hrParams, Metadata);

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
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
