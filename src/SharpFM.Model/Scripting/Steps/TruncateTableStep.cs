using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Truncate Table carries a <c>NoInteract</c> boolean (HR-inverted:
/// <c>state="True"</c> means <c>With dialog: Off</c>) and a <c>BaseTable</c>
/// named reference. The BaseTable can also have an optional <c>comment</c>
/// attribute in FM Pro output which we preserve.
/// </summary>
public sealed class TruncateTableStep : ScriptStep, IStepFactory
{
    public const int XmlId = 182;
    public const string XmlName = "Truncate Table";

    public bool WithDialog { get; set; }
    public NamedRef Table { get; set; }
    public string? TableComment { get; set; }

    public TruncateTableStep(bool withDialog = true, NamedRef? table = null, string? tableComment = null, bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Table = table ?? new NamedRef(-1, "<Current Table>");
        TableComment = tableComment;
    }

    public override XElement ToXml()
    {
        var baseTable = new XElement("BaseTable",
            new XAttribute("id", Table.Id));
        if (TableComment is not null)
            baseTable.Add(new XAttribute("comment", TableComment));
        baseTable.Add(new XAttribute("name", Table.Name));

        return new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "True" : "False")),
            baseTable);
    }

    public override string ToDisplayLine() =>
        $"Truncate Table [ With dialog: {(WithDialog ? "On" : "Off")} ; Table: {Table.Name} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value == "True";
        var tableEl = step.Element("BaseTable");
        var table = tableEl is not null ? NamedRef.FromXml(tableEl) : new NamedRef(-1, "<Current Table>");
        var comment = tableEl?.Attribute("comment")?.Value;
        return new TruncateTableStep(withDialog, table, comment, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool withDialog = true;
        NamedRef? table = null;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
            {
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            }
            else if (t.StartsWith("Table:", StringComparison.OrdinalIgnoreCase))
            {
                var name = t.Substring(6).Trim();
                table = new NamedRef(0, name);
            }
        }
        return new TruncateTableStep(withDialog, table, null, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "records",
        HelpUrl = "https://help.claris.com/en/pro-help/content/truncate-table.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "NoInteract",
                XmlElement = "NoInteract",
                XmlAttr = "state",
                Type = "boolean",
                HrLabel = "With dialog",
                ValidValues = ["On", "Off"],
                DefaultValue = "True",
            },
            new ParamMetadata
            {
                Name = "BaseTable",
                XmlElement = "BaseTable",
                Type = "tableReference",
                HrLabel = "Table",
                Required = true,
            },
        ],
        Notes = new StepNotes
        {
            Behavioral = "Deletes all records in specified source table regardless of current found set. Cannot undo.",
            Gotchas = "Does NOT delete related records even if relationship is set up to do so.",
        },
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
