using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for DeleteFileStep: three &lt;Step&gt; attributes
/// plus a single &lt;UniversalPathList&gt; element carrying the target
/// file path as text. Display form uses the HR-label prefix
/// "Target file:" followed by the raw path string.
/// </summary>
public sealed class DeleteFileStep : ScriptStep, IStepFactory
{
    public const int XmlId = 197;
    public const string XmlName = "Delete File";

    public string TargetFile { get; set; }

    private DeleteFileStep() : base(false) { TargetFile = ""; }

    public DeleteFileStep(string targetFile = "", bool enabled = true) : base(enabled)
    {
        TargetFile = targetFile;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<DeleteFileStep>(step, Metadata);

    // Hand-written: also accepts a bare unlabeled path token, which the
    // shape parser would ignore (labeled slots never bind positionally).
    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tok = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        const string Prefix = "Target file:";
        if (tok.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            tok = tok.Substring(Prefix.Length).Trim();
        return new DeleteFileStep(tok, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName, Id = XmlId, Category = "files",
        HelpUrl = "https://help.claris.com/en/pro-help/content/delete-file.html",
        // Canonical unconfigured form is empty: the path text is omitted when blank.
        Shape =
        [
            new NamedTextChild("UniversalPathList") { PocoProperty = "TargetFile", HrLabel = "Target file", Required = true, Optional = true, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
