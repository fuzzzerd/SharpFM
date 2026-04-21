using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for DeleteRecordRequestStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;NoInteract state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class DeleteRecordRequestStep : ScriptStep, IStepFactory
{
    public const int XmlId = 9;
    public const string XmlName = "Delete Record/Request";

    /// <summary>The <c>With dialog</c> flag on the step.</summary>
    public bool WithDialog { get; set; }

    public DeleteRecordRequestStep(bool withdialog = false, bool enabled = true)
        : base(enabled)
    {
        WithDialog = withdialog;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract",
                new XAttribute("state", WithDialog ? "True" : "False")));

    public override string ToDisplayLine() =>
        $"Delete Record/Request [ With dialog: {(WithDialog ? "Off" : "On")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var state = step.Element("NoInteract")?.Attribute("state")?.Value == "True";
        return new DeleteRecordRequestStep(state, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        const string Prefix = "With dialog:";
        if (token.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            token = token.Substring(Prefix.Length).Trim();
        var isOn = token.Equals("On", StringComparison.OrdinalIgnoreCase);
        return new DeleteRecordRequestStep(!isOn, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/delete-record-request.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "NoInteract",
                XmlElement = "NoInteract",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "With dialog",
                // invertedHr: display 'On' means XML state='False'.
                ValidValues = ["On", "Off"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
