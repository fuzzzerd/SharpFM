using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetSessionIdentifierStep : ScriptStep, IStepFactory
{
    public const int XmlId = 208;
    public const string XmlName = "Set Session Identifier";

    public Calculation SessionIdentifier { get; set; }

    public SetSessionIdentifierStep(
        Calculation? sessionIdentifier = null,
        bool enabled = true)
        : base(null, enabled)
    {
        SessionIdentifier = sessionIdentifier ?? new Calculation("");
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            SessionIdentifier.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Set Session Identifier [ " + "Session identifier: " + SessionIdentifier.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var sessionIdentifier_vEl = step.Element("Calculation");
        var sessionIdentifier_v = sessionIdentifier_vEl is not null ? Calculation.FromXml(sessionIdentifier_vEl) : new Calculation("");
        return new SetSessionIdentifierStep(sessionIdentifier_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? sessionIdentifier_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Session identifier:", StringComparison.OrdinalIgnoreCase)) { sessionIdentifier_v = new Calculation(tok.Substring(19).Trim()); break; } }
        return new SetSessionIdentifierStep(sessionIdentifier_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-session-identifier.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
                HrLabel = "Session identifier",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
