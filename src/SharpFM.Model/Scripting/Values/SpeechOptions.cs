using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// Macros for macOS speech synthesis used by the Speak step. All fields
/// round-trip losslessly; VoiceId and VoiceCreator are numeric but FM
/// preserves them as strings in XML so we keep them as strings too.
/// </summary>
public sealed record SpeechOptions(bool WaitForCompletion, string VoiceName, string VoiceId, string VoiceCreator)
{
    public XElement ToXml()
    {
        // Canonical form carries WaitForCompletion always and only the populated
        // voice attributes; empty optional attributes are not emitted.
        var el = new XElement("SpeechOptions",
            new XAttribute("WaitForCompletion", WaitForCompletion ? "True" : "False"));
        if (!string.IsNullOrEmpty(VoiceName)) el.Add(new XAttribute("VoiceName", VoiceName));
        if (!string.IsNullOrEmpty(VoiceId)) el.Add(new XAttribute("VoiceId", VoiceId));
        if (!string.IsNullOrEmpty(VoiceCreator)) el.Add(new XAttribute("VoiceCreator", VoiceCreator));
        return el;
    }

    public static SpeechOptions FromXml(XElement element) =>
        new(
            element.Attribute("WaitForCompletion")?.Value == "True",
            element.Attribute("VoiceName")?.Value ?? "",
            element.Attribute("VoiceId")?.Value ?? "",
            element.Attribute("VoiceCreator")?.Value ?? "");
}
