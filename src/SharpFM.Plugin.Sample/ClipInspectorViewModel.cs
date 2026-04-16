using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using SharpFM.Model;
using SharpFM.Plugin;

namespace SharpFM.Plugin.Sample;

public class ClipInspectorViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private string _clipName = "(no clip selected)";
    public string ClipName { get => _clipName; private set { _clipName = value; Notify(); } }

    private string _clipType = "-";
    public string ClipType { get => _clipType; private set { _clipType = value; Notify(); } }

    private string _elementCount = "-";
    public string ElementCount { get => _elementCount; private set { _elementCount = value; Notify(); } }

    private string _xmlSize = "-";
    public string XmlSize { get => _xmlSize; private set { _xmlSize = value; Notify(); } }

    private bool _hasClip;
    public bool HasClip { get => _hasClip; private set { _hasClip = value; Notify(); } }

    public void Update(ClipData? clip)
    {
        if (clip is null)
        {
            ClipName = "(no clip selected)";
            ClipType = "-";
            ElementCount = "-";
            XmlSize = "-";
            HasClip = false;
            return;
        }

        HasClip = true;
        ClipName = clip.Name;
        ClipType = clip.ClipType;
        XmlSize = FormatBytes(clip.Xml.Length * 2); // rough UTF-16 estimate

        try
        {
            var doc = XDocument.Parse(clip.Xml);
            var count = doc.Descendants().Count();
            ElementCount = count.ToString();
        }
        catch
        {
            ElementCount = "(invalid XML)";
        }
    }

    private static string FormatBytes(int bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
