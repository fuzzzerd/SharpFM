using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExecuteFileMakerDataApiStep : ScriptStep, IStepFactory
{
    public const int XmlId = 203;
    public const string XmlName = "Execute FileMaker Data API";

    public bool Select { get; set; }
    public FieldRef? Target { get; set; }
    public Calculation? Query { get; set; }

    private ExecuteFileMakerDataApiStep() : base(false) { }

    public ExecuteFileMakerDataApiStep(
        bool select = true,
        FieldRef? target = null,
        Calculation? query = null,
        bool enabled = true)
        : base(enabled)
    {
        Select = select;
        Target = target;
        Query = query;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: FileMaker shows Target before the query calculation, the
    // reverse of the canonical XML order the shape must keep.
    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        parts.Add($"Select: {(Select ? "On" : "Off")}");
        if (Target is not null) parts.Add($"Target: {Target.ToDisplayString()}");
        parts.Add(Query?.Text ?? "");
        return $"Execute FileMaker Data API [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ExecuteFileMakerDataApiStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool select = true;
        FieldRef? target = null;
        Calculation query = new("");
        bool querySeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Select:", StringComparison.OrdinalIgnoreCase))
                select = t.Substring(7).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            else if (!querySeen && !string.IsNullOrWhiteSpace(t))
            {
                query = new Calculation(t);
                querySeen = true;
            }
        }
        return new ExecuteFileMakerDataApiStep(select, target, query, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/execute-filemaker-data-api.html",
        // Canonical: SelectAll, then the optional bare <Calculation> (query) and
        // field target (variable target gets the bare <Text/> marker).
        Shape =
        [
            new BoolStateChild("SelectAll") { PocoProperty = "Select", HrLabel = "Select", Display = DisplayMode.Native },
            new BareCalcChild { PocoProperty = "Query", Optional = true, Display = DisplayMode.Native },
            new FieldChild("Field") { PocoProperty = "Target", HrLabel = "Target", Optional = true, VariableTextMarker = true, Display = DisplayMode.Native },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
