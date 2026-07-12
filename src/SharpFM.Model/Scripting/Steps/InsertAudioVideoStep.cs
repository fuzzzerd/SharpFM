using System;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for InsertAudioVideoStep: three &lt;Step&gt;
/// attributes plus a single &lt;UniversalPathList type="..."&gt; element
/// that carries BOTH the file path as text content AND a "type"
/// attribute marking the payload as embedded or a reference.
/// </summary>
public sealed class InsertAudioVideoStep : ScriptStep<InsertAudioVideoStep>, IStepFactory
{
    public const int XmlId = 159;
    public const string XmlName = "Insert Audio/Video";

    public string Path { get; set; }
    public string Reference { get; set; }

    private InsertAudioVideoStep() : this("") { }

    public InsertAudioVideoStep(
        string path = "",
        string reference = "Embedded",
        bool enabled = true)
        : base(enabled)
    {
        Path = path;
        Reference = reference;
    }

    // Hand-written: the single wire element splits into two display tokens
    // (path text + type attribute), which the shape renderer cannot produce.
    public override string ToDisplayLine() =>
        $"Insert Audio/Video [ {Path} ; Reference: {Reference} ]";

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        string reference = "Embedded";
        foreach (var tok in tokens)
        {
            if (tok.StartsWith("Reference:", StringComparison.OrdinalIgnoreCase))
            {
                reference = tok.Substring("Reference:".Length).Trim();
                break;
            }
        }
        var path = tokens.FirstOrDefault(t =>
            !t.StartsWith("Reference:", StringComparison.OrdinalIgnoreCase)) ?? "";
        Path = path;
        Reference = reference;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName, Id = XmlId, Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-audio-video.html",
        Shape =
        [
            // The wire element surfaces as the labeled Reference slot; the path
            // text it also carries gets its own HR-only positional slot.
            new HrOnly("UniversalPathList"),
            new NamedTextChild("UniversalPathList")
            {
                PocoProperty = "Path",
                Attr = "type",
                AttrProperty = "Reference",
                AttrDefault = "Embedded",
                HrLabel = "Reference",
                ValidValues = ["Embedded", "Reference"],
            },
        ],
    };
}
