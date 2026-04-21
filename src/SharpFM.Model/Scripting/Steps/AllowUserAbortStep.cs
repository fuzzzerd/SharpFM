using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for AllowUserAbortStep: the step's XML state is the three
/// &lt;Step&gt; attributes (enable/id/name) plus a single
/// &lt;Set state="True|False"/&gt; child. All round-tripped.
/// </summary>
public sealed class AllowUserAbortStep : ScriptStep, IStepFactory
{
    public const int XmlId = 85;
    public const string XmlName = "Allow User Abort";

    /// <summary>The boolean setting encoded as <c>&lt;Set state="True|False"/&gt;</c>.</summary>
    public bool Set { get; set; }

    public AllowUserAbortStep(bool set = false, bool enabled = true)
        : base(null, enabled)
    {
        Set = set;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("Set",
                new XAttribute("state", Set ? "True" : "False")));

    public override string ToDisplayLine() =>
        $"Allow User Abort [ {(Set ? "On" : "Off")} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var state = step.Element("Set")?.Attribute("state")?.Value == "True";
        return new AllowUserAbortStep(state, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var isOn = hrParams.Length > 0 && hrParams[0].Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
        return new AllowUserAbortStep(isOn, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        Params =
        [
            new ParamMetadata
            {
                Name = "Set",
                XmlElement = "Set",
                Type = "boolean",
                XmlAttr = "state",
                ValidValues = ["On", "Off"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
