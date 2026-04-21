using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// The <c>&lt;PlatformData&gt;</c> children under Print and Print Setup
/// carry opaque platform-specific payloads (macOS PMPageFormat blobs,
/// etc.). We preserve the PlatformType attribute and CDATA body literally
/// so clipboard round-trip is exact even when we don't know the payload's
/// structure.
/// </summary>
public sealed record PlatformData(string PlatformType, string Content)
{
    public XElement ToXml() =>
        new("PlatformData",
            new XAttribute("PlatformType", PlatformType),
            new XCData(Content));

    public static PlatformData FromXml(XElement element) =>
        new(element.Attribute("PlatformType")?.Value ?? "", element.Value);

    public static IReadOnlyList<PlatformData> ListFromParent(XElement parent) =>
        parent.Elements("PlatformData").Select(FromXml).ToList();
}
