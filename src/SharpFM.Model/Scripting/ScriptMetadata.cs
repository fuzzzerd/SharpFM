using System.Xml.Linq;

namespace SharpFM.Model.Scripting;

/// <summary>
/// Attributes carried on the <c>&lt;Script&gt;</c> wrapper element in a
/// Mac-XMSC ("Script") clipboard payload. A Mac-XMSS ("ScriptSteps")
/// payload has no wrapper and therefore no metadata.
/// <para>
/// Preserving this shape is what makes a round-trip through SharpFM
/// faithful to FM Pro's expectations: pasting a "Script"-flavored clip
/// into FM Pro requires the <c>&lt;Script&gt;</c> wrapper, while a
/// "ScriptSteps" clip must omit it. <see cref="FmScript.ToXml"/> emits
/// the wrapper if and only if a <see cref="ScriptMetadata"/> is present
/// on the <see cref="FmScript"/>.
/// </para>
/// </summary>
public sealed record ScriptMetadata(
    int Id,
    string Name,
    bool IncludeInMenu,
    bool RunFullAccess)
{
    public static ScriptMetadata FromXml(XElement scriptElement)
    {
        var idStr = scriptElement.Attribute("id")?.Value;
        var id = int.TryParse(idStr, out var parsed) ? parsed : 0;
        var name = scriptElement.Attribute("name")?.Value ?? "";
        var includeInMenu = scriptElement.Attribute("includeInMenu")?.Value == "True";
        var runFullAccess = scriptElement.Attribute("runFullAccess")?.Value == "True";
        return new ScriptMetadata(id, name, includeInMenu, runFullAccess);
    }

    public XElement ToXmlElement() =>
        new("Script",
            new XAttribute("includeInMenu", IncludeInMenu ? "True" : "False"),
            new XAttribute("runFullAccess", RunFullAccess ? "True" : "False"),
            new XAttribute("id", Id),
            new XAttribute("name", Name));

    /// <summary>
    /// Sensible defaults for a freshly-created Script wrapper when the
    /// user promotes a ScriptSteps clip to a Script via "Copy as Script"
    /// with no prior metadata.
    /// </summary>
    public static ScriptMetadata Default(string name) =>
        new(Id: 0, Name: name, IncludeInMenu: true, RunFullAccess: false);
}
