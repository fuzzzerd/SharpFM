using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Insert from Device (FileMaker Go only). Captures data from device
/// hardware into a container or text field. The DeviceOptions subtree
/// varies dramatically by InsertFrom value (Camera, Video, Microphone,
/// Photo Library, Music Library, Barcode, Signature) — preserved
/// verbatim via <see cref="StepChildBag"/> for round-trip fidelity.
/// </summary>
public sealed class InsertFromDeviceStep : ScriptStep, IStepFactory
{
    public const int XmlId = 161;
    public const string XmlName = "Insert from Device";

    public string InsertFrom { get; set; }
    public FieldRef Target { get; set; }
    public StepChildBag DeviceOptions { get; set; }

    public InsertFromDeviceStep(
        string insertFrom = "Camera",
        FieldRef? target = null,
        StepChildBag? deviceOptions = null,
        bool enabled = true)
        : base(enabled)
    {
        InsertFrom = insertFrom;
        Target = target ?? FieldRef.ForField("", 0, "");
        DeviceOptions = deviceOptions ?? new StepChildBag();
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("InsertFrom", new XAttribute("value", InsertFrom)),
            Target.ToXml("Field"));
        var opts = new XElement("DeviceOptions");
        DeviceOptions.AppendTo(opts);
        step.Add(opts);
        return step;
    }

    public override string ToDisplayLine() =>
        $"Insert from Device [ {InsertFrom} ; {Target.ToDisplayString()} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var insertFrom = step.Element("InsertFrom")?.Attribute("value")?.Value ?? "Camera";
        var fieldEl = step.Element("Field");
        var target = fieldEl is not null ? FieldRef.FromXml(fieldEl) : FieldRef.ForField("", 0, "");
        var optsEl = step.Element("DeviceOptions");
        var opts = optsEl is not null ? StepChildBag.FromParent(optsEl) : new StepChildBag();
        return new InsertFromDeviceStep(insertFrom, target, opts, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        new InsertFromDeviceStep(enabled: enabled);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-from-device.html",
        Params =
        [
            new ParamMetadata { Name = "InsertFrom", XmlElement = "InsertFrom", XmlAttr = "value", Type = "enum", ValidValues = ["Camera", "Video Camera", "Microphone", "Photo Library", "Music Library", "Barcode", "Signature"] },
            new ParamMetadata { Name = "Field", XmlElement = "Field", Type = "field", Required = true },
            new ParamMetadata { Name = "DeviceOptions", XmlElement = "DeviceOptions", Type = "complex" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
