using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Perform JavaScript in Web Viewer. Calls a JavaScript function in a
/// layout's web viewer with an optional ObjectName target and a parameter
/// list. Parameters are wrapped in <c>&lt;P&gt;&lt;Calculation/&gt;&lt;/P&gt;</c>
/// children under a <c>&lt;Parameters Count="N"&gt;</c> element.
/// </summary>
public sealed class PerformJavaScriptInWebViewerStep : ScriptStep<PerformJavaScriptInWebViewerStep>, IStepFactory
{
    public const int XmlId = 175;
    public const string XmlName = "Perform JavaScript in Web Viewer";

    public Calculation? ObjectName { get; set; }
    public Calculation? FunctionName { get; set; }
    public IReadOnlyList<Calculation> Parameters { get; set; } = new List<Calculation>();

    private PerformJavaScriptInWebViewerStep() : base(false) { }

    public PerformJavaScriptInWebViewerStep(
        Calculation? objectName = null,
        Calculation? functionName = null,
        IReadOnlyList<Calculation>? parameters = null,
        bool enabled = true)
        : base(enabled)
    {
        ObjectName = objectName;
        FunctionName = functionName;
        Parameters = parameters ?? new List<Calculation>();
    }

    // Hand-written: the unconfigured form renders empty brackets and the
    // parameter list joins as one comma-separated token — forms the shape
    // display engine cannot express.
    public override string ToDisplayLine()
    {
        var parts = new List<string>();
        if (ObjectName is not null) parts.Add($"Object Name: {ObjectName.Text}");
        if (FunctionName is not null) parts.Add($"Function Name: {FunctionName.Text}");
        if (Parameters.Count > 0)
            parts.Add($"Parameters: {string.Join(", ", Parameters.Select(p => p.Text))}");
        return $"Perform JavaScript in Web Viewer [ {string.Join(" ; ", parts)} ]";
    }

    protected internal override void PopulateFromDisplay(string[] hrParams)
    {
        Calculation? obj = null;
        Calculation? fn = null;
        List<Calculation> parameters = new();
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("Object Name:", System.StringComparison.OrdinalIgnoreCase))
                obj = new Calculation(t.Substring(12).Trim());
            else if (t.StartsWith("Function Name:", System.StringComparison.OrdinalIgnoreCase))
                fn = new Calculation(t.Substring(14).Trim());
            else if (t.StartsWith("Parameters:", System.StringComparison.OrdinalIgnoreCase))
            {
                var list = t.Substring(11).Trim();
                foreach (var p in list.Split(','))
                    parameters.Add(new Calculation(p.Trim()));
            }
        }
        ObjectName = obj;
        FunctionName = fn;
        Parameters = parameters;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-javascript-in-web-viewer.html",
        // Canonical unconfigured form is empty; ObjectName, FunctionName, and
        // the <Parameters Count><P><Calculation> list (§7.2) are emitted only
        // when set.
        Shape =
        [
            new NamedCalcChild("ObjectName") { PocoProperty = "ObjectName", HrLabel = "Object Name", Optional = true, Display = DisplayMode.Augmented },
            new NamedCalcChild("FunctionName") { PocoProperty = "FunctionName", HrLabel = "Function Name", Optional = true, Display = DisplayMode.Augmented },
            new ParametersList() { PocoProperty = "Parameters", HrLabel = "Parameters", Optional = true, Display = DisplayMode.Augmented },
        ],
    };
}
