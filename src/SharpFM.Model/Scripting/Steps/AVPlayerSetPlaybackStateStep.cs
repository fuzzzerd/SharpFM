using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;

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
    public string PlaybackState { get; set; } = "Stopped";

    private AVPlayerSetPlaybackStateStep() : base(false) { }

    public AVPlayerSetPlaybackStateStep(string playbackState = "Stopped", bool enabled = true)
        : base(enabled)
    {
        PlaybackState = playbackState;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    public override string ToDisplayLine() => StepDisplayRenderer.Render(this, Metadata);

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<AVPlayerSetPlaybackStateStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams) =>
        StepDisplayParser.Parse<AVPlayerSetPlaybackStateStep>(enabled, hrParams, Metadata);

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/avplayer-set-playback-state.html",
        // Single always-emitted <PlaybackState value="..."/> enum child.
        Shape =
        [
            new EnumValueChild("PlaybackState") { PocoProperty = "PlaybackState", DefaultValue = "Stopped", ValidValues = ["Stopped", "Paused", "Playing"] },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
