using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class PerformAppleScriptStep : ScriptStep<PerformAppleScriptStep>, IStepFactory
{
    public const int XmlId = 67;
    public const string XmlName = "Perform AppleScript";

    public string ContentType { get; set; }
    public Calculation? Calculation { get; set; }
    public string Text { get; set; }

    private PerformAppleScriptStep() : base(false) { ContentType = "Calculation"; Text = ""; }

    public PerformAppleScriptStep(
        string contentType = "Calculation",
        Calculation? calculation = null,
        string text = "",
        bool enabled = true)
        : base(enabled)
    {
        ContentType = contentType;
        Calculation = calculation;
        Text = text;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/perform-applescript-os-x.html",
        // Canonical 067-PerformAppleScript: only <ContentType>; the bare
        // <Calculation> and the <Text> element are omitted until configured,
        // so both are Optional.
        Shape =
        [
            new EnumValueChild("ContentType") { PocoProperty = "ContentType", DefaultValue = "Calculation", ValidValues = ["Calculation", "Text"], DisplayValues = ["Calculation", "Text"] },
            new BareCalcChild { PocoProperty = "Calculation", Optional = true, DisplayEmptyAs = "" },
            new NamedTextChild("Text") { PocoProperty = "Text", Optional = true, DisplayEmptyAs = "" },
        ],
    };
}
