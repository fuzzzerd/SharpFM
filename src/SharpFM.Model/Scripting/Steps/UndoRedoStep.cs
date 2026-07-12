using System;
using System.Collections.Generic;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for UndoRedoStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;UndoRedo value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class UndoRedoStep : ScriptStep<UndoRedoStep>, IStepFactory
{
    public const int XmlId = 45;
    public const string XmlName = "Undo/Redo";

    /// <summary>The enum XML value emitted on the <c>&lt;UndoRedo&gt;</c> element.</summary>
    public string Action { get; set; }

    private UndoRedoStep() : base(false)
    {
        Action = "Undo";
    }

    public UndoRedoStep(string action = "Undo", bool enabled = true)
        : base(enabled)
    {
        Action = action;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "editing",
        HelpUrl = "https://help.claris.com/en/pro-help/content/undo-redo.html",
        Shape =
        [
            new EnumValueChild("UndoRedo") { PocoProperty = "Action", HrLabel = "Action", DefaultValue = "Undo", ValidValues = ["Undo", "Redo", "Toggle"] },
        ],
    };
}
