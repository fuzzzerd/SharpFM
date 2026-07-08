using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetSessionIdentifierStep : ScriptStep, IStepFactory
{
    public const int XmlId = 208;
    public const string XmlName = "Set Session Identifier";

    public Calculation? SessionIdentifier { get; set; }

    private SetSessionIdentifierStep() : base(false) { }

    public SetSessionIdentifierStep(
        Calculation? sessionIdentifier = null,
        bool enabled = true)
        : base(enabled)
    {
        SessionIdentifier = sessionIdentifier;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetSessionIdentifierStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<SetSessionIdentifierStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-session-identifier.html",
        // Canonical unconfigured form is empty: the bare identifier calc is omitted when blank.
        Shape =
        [
            new BareCalcChild { PocoProperty = "SessionIdentifier", HrLabel = "Session identifier", Optional = true, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
