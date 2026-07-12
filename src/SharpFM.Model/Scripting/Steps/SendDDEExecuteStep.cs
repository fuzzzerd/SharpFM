using System;
using System.Collections.Generic;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for SendDDEExecuteStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;ContentType value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class SendDDEExecuteStep : ScriptStep<SendDDEExecuteStep>, IStepFactory
{
    public const int XmlId = 64;
    public const string XmlName = "Send DDE Execute";

    /// <summary>The enum XML value emitted on the <c>&lt;ContentType&gt;</c> element.</summary>
    public string ContentType { get; set; }

    private SendDDEExecuteStep() : base(false)
    {
        ContentType = "File";
    }

    public SendDDEExecuteStep(string contentType = "File", bool enabled = true)
        : base(enabled)
    {
        ContentType = contentType;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/send-dde-execute-windows.html",
        Shape =
        [
            new EnumValueChild("ContentType") { PocoProperty = "ContentType", DefaultValue = "File", ValidValues = ["File"] },
        ],
    };
}
