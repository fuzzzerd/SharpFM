using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for ShowHideMenubarStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus one child element per metadata param.
/// Display form uses labeled segments joined by ' ; '; FromDisplayParams
/// scans tokens by label so segment order is free.
/// </summary>
public sealed class ShowHideMenubarStep : ScriptStep, IStepFactory
{
    public const int XmlId = 166;
    public const string XmlName = "Show/Hide Menubar";

    public bool Lock { get; set; }
    public string Action { get; set; }

    private ShowHideMenubarStep() : base(false)
    {
        Action = "Hide";
    }

    public ShowHideMenubarStep(
        bool @lock = false,
        string action = "Hide",
        bool enabled = true)
        : base(enabled)
    {
        Lock = @lock;
        Action = action;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<ShowHideMenubarStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<ShowHideMenubarStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/show-hide-menubar.html",
        Shape =
        [
            new BoolStateChild("Lock") { PocoProperty = "Lock", HrLabel = "Lock" },
            new EnumValueChild("ShowHide") { PocoProperty = "Action", HrLabel = "Action", ValidValues = ["Show", "Hide", "Toggle"] },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
