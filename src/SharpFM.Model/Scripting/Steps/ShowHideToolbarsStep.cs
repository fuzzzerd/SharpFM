using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ShowHideToolbarsStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus one child element per metadata param.
/// Display form uses labeled segments joined by ' ; '; the shape-driven
/// display parser scans tokens by label so segment order is free.
/// </summary>
public sealed class ShowHideToolbarsStep : ScriptStep<ShowHideToolbarsStep>, IStepFactory
{
    public const int XmlId = 29;
    public const string XmlName = "Show/Hide Toolbars";

    public bool IncludeEditRecordToolbar { get; set; }
    public bool Lock { get; set; }
    public string Action { get; set; }

    private ShowHideToolbarsStep() : base(false)
    {
        Action = "Hide";
    }

    public ShowHideToolbarsStep(
        bool includeEditRecordToolbar = false,
        bool @lock = false,
        string action = "Hide",
        bool enabled = true)
        : base(enabled)
    {
        IncludeEditRecordToolbar = includeEditRecordToolbar;
        Lock = @lock;
        Action = action;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/show-hide-toolbars.html",
        Shape =
        [
            new BoolStateChild("IncludeEditRecordToolbar") { HrLabel = "Include Edit Record Toolbar" },
            new BoolStateChild("Lock") { HrLabel = "Lock" },
            new EnumValueChild("ShowHide") { PocoProperty = "Action", HrLabel = "Action", ValidValues = ["Show", "Hide", "Toggle"], DefaultValue = "Hide" },
        ],
    };
}
