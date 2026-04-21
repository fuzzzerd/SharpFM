using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Values;

/// <summary>
/// Macros for macOS speech synthesis used by the Speak step. All fields
/// round-trip losslessly; VoiceId and VoiceCreator are numeric but FM
/// preserves them as strings in XML so we keep them as strings too.
/// </summary>
public sealed record SpeechOptions(bool WaitForCompletion, string VoiceName, string VoiceId, string VoiceCreator)
{
    public XElement ToXml() =>
        new("SpeechOptions",
            new XAttribute("WaitForCompletion", WaitForCompletion ? "True" : "False"),
            new XAttribute("VoiceName", VoiceName),
            new XAttribute("VoiceId", VoiceId),
            new XAttribute("VoiceCreator", VoiceCreator));

    public static SpeechOptions FromXml(XElement element) =>
        new(
            element.Attribute("WaitForCompletion")?.Value == "True",
            element.Attribute("VoiceName")?.Value ?? "",
            element.Attribute("VoiceId")?.Value ?? "",
            element.Attribute("VoiceCreator")?.Value ?? "");
}
