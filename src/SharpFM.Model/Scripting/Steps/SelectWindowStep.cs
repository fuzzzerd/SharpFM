using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SelectWindowStep : ScriptStep, IStepFactory
{
    public const int XmlId = 123;
    public const string XmlName = "Select Window";

    public bool CurrentFile { get; set; }
    public string Window { get; set; } = "ByName";
    public Calculation Name { get; set; } = new("");

    private SelectWindowStep() : base(false) { }

    public SelectWindowStep(
        bool currentFile = true,
        string window = "ByName",
        Calculation? name = null,
        bool enabled = true)
        : base(enabled)
    {
        CurrentFile = currentFile;
        Window = window;
        Name = name ?? new Calculation("");
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<SelectWindowStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<SelectWindowStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "windows",
        HelpUrl = "https://help.claris.com/en/pro-help/content/select-window.html",
        // Canonical: LimitToWindowsOfCurrentFile, Window, then the optional
        // <Name> calculation (present only in the ByName form).
        Shape =
        [
            new BoolStateChild("LimitToWindowsOfCurrentFile") { PocoProperty = "CurrentFile", HrLabel = "Current file", Display = DisplayMode.Native },
            new EnumValueChild("Window") { PocoProperty = "Window", HrLabel = "Window", DefaultValue = "ByName", ValidValues = ["ByName", "Current"], DisplayValues = ["Name: <calc>", "Current Window"], Display = DisplayMode.Native },
            new NamedCalcChild("Name") { PocoProperty = "Name", HrLabel = "Name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
