using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetWebViewerStep : ScriptStep, IStepFactory
{
    public const int XmlId = 146;
    public const string XmlName = "Set Web Viewer";

    public Calculation ObjectName { get; set; }
    public string Action { get; set; }
    public Calculation URL { get; set; }

    /// <summary>
    /// Shape-facing view of the <c>&lt;URL&gt;</c> element (a calc wrapper that
    /// also carries a <c>custom</c> attribute, which no shape primitive
    /// models): emitted only when a URL is configured and parsed back through
    /// the shape's passthrough slot. The attribute is always written as
    /// <c>"False"</c>, matching FM Pro's canonical form.
    /// </summary>
    public List<XElement> UrlWire
    {
        get => string.IsNullOrEmpty(URL.Text)
            ? []
            : [new XElement("URL", new XAttribute("custom", "False"), URL.ToXml("Calculation"))];
        set
        {
            var calcEl = value.FirstOrDefault(e => e.Name.LocalName == "URL")?.Element("Calculation");
            URL = calcEl is not null ? Calculation.FromXml(calcEl) : new Calculation("");
        }
    }

    private SetWebViewerStep() : this(enabled: true) { }

    public SetWebViewerStep(
        Calculation? objectName = null,
        string action = "GoToURL",
        Calculation? uRL = null,
        bool enabled = true)
        : base(enabled)
    {
        ObjectName = objectName ?? new Calculation("");
        Action = action;
        URL = uRL ?? new Calculation("");
    }

    private static readonly IReadOnlyDictionary<string, string> _ActionToHr =
        new Dictionary<string, string>(StringComparer.Ordinal) {
        ["GoToURL"] = "Go to URL",
        ["Reset"] = "Reset",
        ["Reload"] = "Reload",
        ["GoForward"] = "Go Forward",
        ["GoBack"] = "Go Back",
    };
    private static readonly IReadOnlyDictionary<string, string> _ActionFromHr =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        ["Go to URL"] = "GoToURL",
        ["Reset"] = "Reset",
        ["Reload"] = "Reload",
        ["Go Forward"] = "GoForward",
        ["Go Back"] = "GoBack",
    };
    private static string ActionHr(string x) => _ActionToHr.TryGetValue(x, out var h) ? h : x;
    private static string ActionXml(string h) => _ActionFromHr.TryGetValue(h, out var x) ? x : h;

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() =>
        "Set Web Viewer [ " + "Object Name: " + ObjectName.Text + " ; " + "Action: " + ActionHr(Action) + " ; " + "URL: " + URL.Text + " ]";

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetWebViewerStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? objectName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Object Name:", StringComparison.OrdinalIgnoreCase)) { objectName_v = new Calculation(tok.Substring(12).Trim()); break; } }
        string action_v = "GoToURL";
        foreach (var tok in tokens) { if (tok.StartsWith("Action:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(7).Trim(); action_v = ActionXml(v); break; } }
        Calculation? uRL_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("URL:", StringComparison.OrdinalIgnoreCase)) { uRL_v = new Calculation(tok.Substring(4).Trim()); break; } }
        return new SetWebViewerStep(objectName_v, action_v, uRL_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-web-viewer.html",
        Shape =
        [
            // Canonical order: Action first, then the optional ObjectName and URL.
            new EnumValueChild("Action") { HrLabel = "Action", ValidValues = ["GoToURL", "Reset", "Reload", "GoForward", "GoBack"], DefaultValue = "GoToURL" },
            new NamedCalcChild("ObjectName") { Optional = true, HrLabel = "Object Name" },
            new Passthrough { PocoProperty = "UrlWire" },
        ],
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Object Name",
            },
            new ParamMetadata
            {
                Name = "Action",
                XmlElement = "Action",
                Type = "enum",
                XmlAttr = "value",
                HrLabel = "Action",
                ValidValues = ["Go to URL", "Reset", "Reload", "Go Forward", "Go Back"],
                DefaultValue = "GoToURL",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "URL",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
