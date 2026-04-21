using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PauseResumeScriptStep : ScriptStep, IStepFactory
{
    public const int XmlId = 62;
    public const string XmlName = "Pause/Resume Script";

    public string PauseTime { get; set; }
    public Calculation DurationSeconds { get; set; }

    public PauseResumeScriptStep(
        string pauseTime = "ForDuration",
        Calculation? durationSeconds = null,
        bool enabled = true)
        : base(null, enabled)
    {
        PauseTime = pauseTime;
        DurationSeconds = durationSeconds ?? new Calculation("");
    }

    private static readonly IReadOnlyDictionary<string, string> _PauseTimeToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["Indefinitely"] = "Indefinitely",
        ["ForDuration"] = "Duration (seconds)",
    };
    private static readonly IReadOnlyDictionary<string, string> _PauseTimeFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["Indefinitely"] = "Indefinitely",
        ["Duration (seconds)"] = "ForDuration",
    };
    private static string PauseTimeHr(string x) => _PauseTimeToHr.TryGetValue(x, out var h) ? h : x;
    private static string PauseTimeXml(string h) => _PauseTimeFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("PauseTime", new XAttribute("value", PauseTime)),
            DurationSeconds.ToXml("Calculation"));

    public override string ToDisplayLine() =>
        "Pause/Resume Script [ " + PauseTimeHr(PauseTime) + " ; " + "Duration (seconds): " + DurationSeconds.Text + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var pauseTime_v = step.Element("PauseTime")?.Attribute("value")?.Value ?? "ForDuration";
        var durationSeconds_vEl = step.Element("Calculation");
        var durationSeconds_v = durationSeconds_vEl is not null ? Calculation.FromXml(durationSeconds_vEl) : new Calculation("");
        return new PauseResumeScriptStep(pauseTime_v, durationSeconds_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string pauseTime_v = "ForDuration";
        Calculation? durationSeconds_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Duration (seconds):", StringComparison.OrdinalIgnoreCase)) { durationSeconds_v = new Calculation(tok.Substring(19).Trim()); break; } }
        return new PauseResumeScriptStep(pauseTime_v, durationSeconds_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "control",
        HelpUrl = "https://help.claris.com/en/pro-help/content/pause-resume-script.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "PauseTime",
                XmlElement = "PauseTime",
                Type = "enum",
                XmlAttr = "value",
                ValidValues = ["Indefinitely", "Duration (seconds)"],
                DefaultValue = "ForDuration",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "calculation",
                HrLabel = "Duration (seconds)",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
