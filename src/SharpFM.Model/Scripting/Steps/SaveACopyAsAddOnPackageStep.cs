using System;
using System.Collections.Generic;
using System.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class SaveACopyAsAddOnPackageStep : ScriptStep<SaveACopyAsAddOnPackageStep>, IStepFactory
{
    public const int XmlId = 96;
    public const string XmlName = "Save a Copy as Add-on Package";

    public bool ReplaceUUIDs { get; set; }
    public Calculation WindowName { get; set; } = new("");

    private SaveACopyAsAddOnPackageStep() : base(false) { }

    public SaveACopyAsAddOnPackageStep(
        bool replaceUUIDs = false,
        Calculation? windowName = null,
        bool enabled = true)
        : base(enabled)
    {
        ReplaceUUIDs = replaceUUIDs;
        WindowName = windowName ?? new Calculation("");
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/save-a-copy-as-add-on-package.html",
        // Canonical: LinkAvail then the window-name Calculation, which the
        // unconfigured form omits (Optional).
        Shape =
        [
            new BoolStateChild("LinkAvail") { PocoProperty = "ReplaceUUIDs", HrLabel = "Replace UUIDs", Display = DisplayMode.Native },
            new BareCalcChild { PocoProperty = "WindowName", HrLabel = "Window name", Optional = true, Display = DisplayMode.Native, DisplayEmptyAs = "" },
        ],
    };
}
