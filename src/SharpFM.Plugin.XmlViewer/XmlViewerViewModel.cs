using System.ComponentModel;
using System.Runtime.CompilerServices;
using AvaloniaEdit.Document;
using SharpFM.Model;
using SharpFM.Plugin;

namespace SharpFM.Plugin.XmlViewer;

public class XmlViewerViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly IPluginHost _host;
    private readonly string _pluginId;
    private bool _isSyncing;

    public TextDocument Document { get; } = new();

    private bool _hasClip;
    public bool HasClip { get => _hasClip; private set { _hasClip = value; Notify(); } }

    private string _clipLabel = "No clip selected";
    public string ClipLabel { get => _clipLabel; private set { _clipLabel = value; Notify(); } }

    public XmlViewerViewModel(IPluginHost host, string pluginId)
    {
        _host = host;
        _pluginId = pluginId;
        Document.TextChanged += OnDocumentTextChanged;
    }

    public void RefreshFromHost()
    {
        var clip = _host.RefreshSelectedClip();
        LoadClip(clip);
    }

    public void LoadClip(ClipData? clip)
    {
        _isSyncing = true;
        try
        {
            if (clip is null)
            {
                Document.Text = "";
                HasClip = false;
                ClipLabel = "No clip selected";
            }
            else
            {
                Document.Text = clip.Xml ?? "";
                HasClip = true;
                ClipLabel = $"{clip.Name} ({clip.ClipType})";
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    public void SyncToHost()
    {
        if (!HasClip) return;
        _isSyncing = true;
        try
        {
            _host.UpdateSelectedClipXml(Document.Text, _pluginId);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void OnDocumentTextChanged(object? sender, System.EventArgs e)
    {
        if (!_isSyncing && HasClip)
        {
            _isSyncing = true;
            try
            {
                _host.UpdateSelectedClipXml(Document.Text, _pluginId);
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}
