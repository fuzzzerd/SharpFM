using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ExecuteSqlStep : ScriptStep, IStepFactory
{
    public const int XmlId = 117;
    public const string XmlName = "Execute SQL";

    public bool WithDialog { get; set; }
    public SqlProfile? Profile { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    private ExecuteSqlStep() : base(false) { }

    public ExecuteSqlStep(bool withDialog = true, SqlProfile? profile = null, bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Profile = profile;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine()
    {
        var dialog = $"With dialog: {(WithDialog ? "On" : "Off")}";
        if (Profile is null) return $"Execute SQL [ {dialog} ]";
        if (Profile.QueryType == "Calculation" && Profile.QueryCalc is not null)
            return $"Execute SQL [ {dialog} ; Calculated SQL Text: {Profile.QueryCalc.Text} ]";
        return $"Execute SQL [ {dialog} ; SQL Text: {Profile.Query ?? ""} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ExecuteSqlStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Display is lossy for Execute SQL — ODBC connection fields aren't
        // expressible in display. Best-effort: capture dialog and SQL text.
        bool withDialog = true;
        string sql = "";
        bool isCalc = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Calculated SQL Text:", StringComparison.OrdinalIgnoreCase))
            {
                sql = t.Substring(20).Trim();
                isCalc = true;
            }
            else if (t.StartsWith("SQL Text:", StringComparison.OrdinalIgnoreCase))
                sql = t.Substring(9).Trim();
        }
        var profile = new SqlProfile(
            isCalc ? "Calculation" : "Query",
            "0", "", "", "", "\t", "-1", "0", "ODBC",
            isCalc ? null : sql,
            isCalc ? new Calculation(sql) : null);
        return new ExecuteSqlStep(withDialog, profile, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/execute-sql.html",
        // Canonical: NoInteract (inverts WithDialog), then the optional ODBC
        // Profile which owns its own attribute/child shape.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog", Display = DisplayMode.Augmented },
            new ValueTypeChild("Profile") { PocoProperty = "Profile", Optional = true, Display = DisplayMode.Hidden },
        ],
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog", ValidValues = ["On", "Off"] },
            new ParamMetadata { Name = "Profile", XmlElement = "Profile", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
