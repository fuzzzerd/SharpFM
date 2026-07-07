using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExportFieldContentsStep : ScriptStep, IStepFactory
{
    public const int XmlId = 132;
    public const string XmlName = "Export Field Contents";

    public FieldRef? Field { get; set; }
    public string Path { get; set; }
    public bool AutoOpen { get; set; }
    public bool CreateEmail { get; set; }
    public bool CreateDirectories { get; set; }

    private ExportFieldContentsStep() : base(false) { }

    public ExportFieldContentsStep(
        FieldRef? field = null,
        string path = "",
        bool autoOpen = false,
        bool createEmail = false,
        bool createDirectories = true,
        bool enabled = true)
        : base(enabled)
    {
        Field = field;
        Path = path;
        AutoOpen = autoOpen;
        CreateEmail = createEmail;
        CreateDirectories = createDirectories;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (Field is not null) parts.Add(Field.ToDisplayString());
        parts.Add(Path);
        if (AutoOpen) parts.Add("Automatically open");
        if (CreateEmail) parts.Add("Create email");
        parts.Add($"Create folders: {(CreateDirectories ? "On" : "Off")}");
        return $"Export Field Contents [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ExportFieldContentsStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        FieldRef? field = null;
        string path = "";
        bool autoOpen = false;
        bool createEmail = false;
        bool createDirs = true;
        int positional = 0;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Automatically open", StringComparison.OrdinalIgnoreCase))
                autoOpen = true;
            else if (t.Equals("Create email", StringComparison.OrdinalIgnoreCase))
                createEmail = true;
            else if (t.StartsWith("Create folders:", StringComparison.OrdinalIgnoreCase))
                createDirs = t.Substring(15).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (!string.IsNullOrWhiteSpace(t))
            {
                if (positional == 0) field = FieldRef.FromDisplayToken(t);
                else if (positional == 1) path = t;
                positional++;
            }
        }
        return new ExportFieldContentsStep(field, path, autoOpen, createEmail, createDirs, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/export-field-contents.html",
        // Canonical order: CreateDirectories, AutoOpen, CreateEmail, then the
        // path list and Field which the unconfigured form omits (Optional).
        Shape =
        [
            new BoolStateChild("CreateDirectories") { PocoProperty = "CreateDirectories", HrLabel = "Create folders", Display = DisplayMode.Augmented },
            new BoolStateChild("AutoOpen") { PocoProperty = "AutoOpen", HrLabel = "Automatically open", Display = DisplayMode.Augmented },
            new BoolStateChild("CreateEmail") { PocoProperty = "CreateEmail", HrLabel = "Create email", Display = DisplayMode.Augmented },
            new NamedTextChild("UniversalPathList") { PocoProperty = "Path", Optional = true, Display = DisplayMode.Native },
            new FieldChild("Field") { PocoProperty = "Field", Optional = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
