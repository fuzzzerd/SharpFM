using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SetWindowTitleStep : ScriptStep, IStepFactory
{
    public const int XmlId = 124;
    public const string XmlName = "Set Window Title";

    public string Window { get; set; } = "ByName";
    public Calculation OfWindow { get; set; } = new("");
    public bool CurrentFile { get; set; }
    public Calculation NewTitle { get; set; } = new("");

    private SetWindowTitleStep() : base(false) { }

    public SetWindowTitleStep(
        string window = "ByName",
        Calculation? ofWindow = null,
        bool currentFile = true,
        Calculation? newTitle = null,
        bool enabled = true)
        : base(enabled)
    {
        Window = window;
        OfWindow = ofWindow ?? new Calculation("");
        CurrentFile = currentFile;
        NewTitle = newTitle ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SetWindowTitleStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<SetWindowTitleStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/set-window-title.html",
        // Canonical: LimitToWindowsOfCurrentFile leads, then Window, then the
        // optional <Name> (of-window) and <NewName> (new-title) calculations.
        Shape =
        [
            // Native slots (Window, Of Window) lead the display line in shape
            // order; Augmented (Current file, New Title) follow — XML order unchanged.
            new BoolStateChild("LimitToWindowsOfCurrentFile") { PocoProperty = "CurrentFile", HrLabel = "Current file", Display = DisplayMode.Augmented },
            new EnumValueChild("Window") { PocoProperty = "Window", HrLabel = "Window", DefaultValue = "ByName", ValidValues = ["Current", "ByName"], DisplayValues = ["Current Window", "Of Window"], Display = DisplayMode.Native },
            new NamedCalcChild("Name") { PocoProperty = "OfWindow", HrLabel = "Of Window", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
            new NamedCalcChild("NewName") { PocoProperty = "NewTitle", HrLabel = "New Title", Optional = true, Display = DisplayMode.Augmented, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
