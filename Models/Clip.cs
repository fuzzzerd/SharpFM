namespace SharpFM.Models;

/// <summary>
/// Clip Data Model
/// </summary>
public class Clip
{
    /// <summary>
    /// Display name for clip may match Name inside the xml data or may not.
    /// </summary>
    public string ClipName { get; set; } = string.Empty;

    /// <summary>
    /// The data format to use when putting the data back on the clipboard for FileMaker.
    /// </summary>
    public string ClipType { get; set; } = string.Empty;

    /// <summary>
    /// Raw xml data from the clip.
    /// </summary>
    public string ClipXml { get; set; } = string.Empty;
}
