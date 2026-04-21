using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SetRevertTransactionOnErrorStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Set state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class SetRevertTransactionOnErrorStep : ScriptStep, IStepFactory
{
    public const int XmlId = 223;
    public const string XmlName = "Set Revert Transaction on Error";

    /// <summary>The <c>Revert on error</c> flag on the step.</summary>
    public bool RevertOnError { get; set; }

    public SetRevertTransactionOnErrorStep(bool revertonerror = false, bool enabled = true)
        : base(enabled)
    {
        RevertOnError = revertonerror;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Set",
                new XAttribute("state", RevertOnError ? "True" : "False")));

    public override string ToDisplayLine() =>
        $"Set Revert Transaction on Error [ Revert on error: {(RevertOnError ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var state = step.Element("Set")?.Attribute("state")?.Value == "True";
        return new SetRevertTransactionOnErrorStep(state, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        const string Prefix = "Revert on error:";
        if (token.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            token = token.Substring(Prefix.Length).Trim();
        var isOn = token.Equals("On", StringComparison.OrdinalIgnoreCase);
        return new SetRevertTransactionOnErrorStep(isOn, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-revert-transaction-on-error.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "Set",
                XmlElement = "Set",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Revert on error",
                ValidValues = ["On", "Off"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
