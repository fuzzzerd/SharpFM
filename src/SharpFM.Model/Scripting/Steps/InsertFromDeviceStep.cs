using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert from Device (FileMaker Go only). Captures data from device
/// hardware into a container or text field. The DeviceOptions subtree
/// varies dramatically by InsertFrom value (Camera, Video, Microphone,
/// Photo Library, Music Library, Barcode, Signature) — preserved
/// verbatim via <see cref="StepChildBag"/> for round-trip fidelity.
/// </summary>
public sealed class InsertFromDeviceStep : ScriptStep<InsertFromDeviceStep>, IStepFactory
{
    public const int XmlId = 161;
    public const string XmlName = "Insert from Device";

    public string InsertFrom { get; set; }
    public FieldRef? Target { get; set; }
    public StepChildBag DeviceOptions { get; set; }

    private InsertFromDeviceStep() : base(false)
    {
        InsertFrom = "Camera";
        DeviceOptions = new StepChildBag();
    }

    public InsertFromDeviceStep(
        string insertFrom = "Camera",
        FieldRef? target = null,
        StepChildBag? deviceOptions = null,
        bool enabled = true)
        : base(enabled)
    {
        InsertFrom = insertFrom;
        Target = target;
        DeviceOptions = deviceOptions ?? new StepChildBag();
    }

    /// <summary>
    /// Display edits are anchor-preserved when a device-specific options
    /// subtree is present — the display line shows only the source and
    /// target, never the DeviceOptions children.
    /// </summary>
    public override bool IsFullyEditable => DeviceOptions.Children.Count == 0;

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-from-device.html",
        // InsertFrom, optional target Field, then the always-present
        // DeviceOptions wrapper whose device-specific subtree is preserved
        // verbatim (preserve-don't-synthesize).
        Shape =
        [
            new EnumValueChild("InsertFrom") { PocoProperty = "InsertFrom", DefaultValue = "Camera", ValidValues = ["Camera", "Video Camera", "Microphone", "Photo Library", "Music Library", "Barcode", "Signature"] },
            new FieldChild("Field") { PocoProperty = "Target", Optional = true },
            new WrapperChild("DeviceOptions",
            [
                new Passthrough { PocoProperty = "DeviceOptions" },
            ]),
            new HrOnly("DeviceOptions"),
        ],
    };
}
