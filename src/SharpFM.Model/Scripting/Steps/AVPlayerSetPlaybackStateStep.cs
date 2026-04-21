using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Zero-loss audit for AVPlayerSetPlaybackStateStep: the step's XML state is the three
/// &lt;Step&gt; attributes plus a single &lt;PlaybackState value="..."/&gt;
/// enum child. XML values and human-readable display values are mapped
/// through the static HR tables; round-trip preserves the XML value.
/// </summary>
public sealed class AVPlayerSetPlaybackStateStep : ScriptStep, IStepFactory
{
    public const int XmlId = 178;
    public const string XmlName = "AVPlayer Set Playback State";

    /// <summary>The enum XML value emitted on the <c>&lt;PlaybackState&gt;</c> element.</summary>
    public string PlaybackState { get; set; }

    public AVPlayerSetPlaybackStateStep(string playbackState = "Stopped", bool enabled = true)
        : base(enabled)
    {
        PlaybackState = playbackState;
    }

    private static readonly IReadOnlyDictionary<string, string> _xmlToHr =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Stopped"] = "Stopped",
        ["Paused"] = "Paused",
        ["Playing"] = "Playing",
    };

    private static readonly IReadOnlyDictionary<string, string> _hrToXml =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Stopped"] = "Stopped",
        ["Paused"] = "Paused",
        ["Playing"] = "Playing",
    };

    private static string ToHr(string xmlValue) =>
        _xmlToHr.TryGetValue(xmlValue, out var hr) ? hr : xmlValue;

    private static string FromHr(string hrValue) =>
        _hrToXml.TryGetValue(hrValue, out var xml) ? xml : hrValue;

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("PlaybackState",
                new XAttribute("value", PlaybackState)));

    public override string ToDisplayLine() =>
        $"AVPlayer Set Playback State [ {ToHr(PlaybackState)} ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var value = step.Element("PlaybackState")?.Attribute("value")?.Value ?? "Stopped";
        return new AVPlayerSetPlaybackStateStep(value, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var token = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        return new AVPlayerSetPlaybackStateStep(FromHr(token), enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/avplayer-set-playback-state.html",
        Params =
        [
            new ParamMetadata
            {
                Name = "PlaybackState",
                XmlElement = "PlaybackState",
                Type = "enum",
                XmlAttr = "value",
                DefaultValue = "Stopped",
                ValidValues = ["Stopped", "Paused", "Playing"],
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
