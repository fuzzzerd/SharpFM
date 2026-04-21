using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InsertFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 131;
    public const string XmlName = "Insert File";

    public string Path { get; set; }
    public string PathType { get; set; }
    public FieldRef? Target { get; set; }
    public InsertFileDialogOptions? DialogOptions { get; set; }

    public InsertFileStep(
        string path = "",
        string pathType = "Embedded",
        FieldRef? target = null,
        InsertFileDialogOptions? dialogOptions = null,
        bool enabled = true)
        : base(null, enabled)
    {
        Path = path;
        PathType = pathType;
        Target = target;
        DialogOptions = dialogOptions;
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("UniversalPathList", new XAttribute("type", PathType), Path));
        if (Target is not null)
        {
            if (Target.IsVariable) step.Add(new XElement("Text"));
            step.Add(Target.ToXml("Field"));
        }
        if (DialogOptions is not null) step.Add(DialogOptions.ToXml());
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (DialogOptions is not null)
        {
            var storage = DialogOptions.StorageType switch
            {
                "InsertOnly" => "Insert",
                "ReferenceOnly" => "Reference",
                "InsertAndReference" => "Insert and Reference",
                _ => null,
            };
            if (storage is not null) parts.Add(storage);
            if (!DialogOptions.AsFile) parts.Add("Display content");
            var compress = DialogOptions.CompressType switch
            {
                "WhenPossible" => "Compress when possible",
                "Never" => "Never compress",
                _ => null,
            };
            if (compress is not null) parts.Add(compress);
        }
        if (Target is not null) parts.Add($"Target: {Target.ToDisplayString()}");
        if (!string.IsNullOrEmpty(Path)) parts.Add(Path);
        return parts.Count == 0 ? "Insert File" : $"Insert File [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var pathEl = step.Element("UniversalPathList");
        var path = pathEl?.Value ?? "";
        var pathType = pathEl?.Attribute("type")?.Value ?? "Embedded";
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : null;
        var dialogEl = step.Element("DialogOptions");
        var dialog = dialogEl is not null ? InsertFileDialogOptions.FromXml(dialogEl) : null;
        return new InsertFileStep(path, pathType, target, dialog, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Display is lossy for Insert File — DialogOptions attributes don't
        // all round-trip. Best-effort: capture path and target.
        string path = "";
        FieldRef? target = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            else if (!string.IsNullOrWhiteSpace(t) && !t.Contains(':') && t != "Insert" && t != "Reference" && t != "Display content" && !t.Contains("ompress"))
                path = t;
        }
        return new InsertFileStep(path, "Embedded", target, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-file.html",
        Params =
        [
            new ParamMetadata { Name = "UniversalPathList", XmlElement = "UniversalPathList", Type = "text" },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "fieldOrVariable", HrLabel = "Target" },
            new ParamMetadata { Name = "DialogOptions", XmlElement = "DialogOptions", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
