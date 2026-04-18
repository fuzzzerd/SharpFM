using SharpFM.Model;

namespace SharpFM.Plugin;

/// <summary>
/// Event arguments for <see cref="IPluginHost.ClipContentChanged"/>.
/// </summary>
/// <param name="Clip">Fresh snapshot of the clip with synced XML.</param>
/// <param name="Origin">"editor" for user edits, or the originating plugin's <see cref="IPanelPlugin.Id"/>.</param>
/// <param name="IsPartial">True if the XML was produced from an incomplete parse (e.g. mid-typing).</param>
public record ClipContentChangedArgs(ClipData Clip, string Origin, bool IsPartial);
