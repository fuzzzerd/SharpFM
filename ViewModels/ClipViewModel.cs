using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AvaloniaEdit.Document;
using SharpFM.Core.ScriptConverter;

namespace SharpFM.ViewModels;

public partial class ClipViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public FileMakerClip Clip { get; set; }

    private TextDocument? _xmlDocument;
    private TextDocument? _scriptDocument;
    private int _selectedEditorTab;
    private bool _isSyncing;

    public ClipViewModel(FileMakerClip clip)
    {
        Clip = clip;
    }

    public bool IsScriptClip =>
        Clip.ClipboardFormat == "Mac-XMSS" || Clip.ClipboardFormat == "Mac-XMSC";

    public string ClipType
    {
        get => Clip.ClipboardFormat;
        set
        {
            Clip.ClipboardFormat = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(IsScriptClip));
        }
    }

    public string Name
    {
        get => Clip.Name;
        set
        {
            Clip.Name = value;
            NotifyPropertyChanged();
        }
    }

    public TextDocument XmlDocument
    {
        get
        {
            if (_xmlDocument == null)
            {
                _xmlDocument = new TextDocument(Clip.XmlData ?? string.Empty);
            }
            return _xmlDocument;
        }
    }

    public TextDocument ScriptDocument
    {
        get
        {
            if (_scriptDocument == null)
            {
                var hr = IsScriptClip ? XmlToHrConverter.Convert(Clip.XmlData ?? "") : "";
                _scriptDocument = new TextDocument(hr);
            }
            return _scriptDocument;
        }
    }

    public int SelectedEditorTab
    {
        get => _selectedEditorTab;
        set
        {
            if (_selectedEditorTab == value) return;
            var previousTab = _selectedEditorTab;
            _selectedEditorTab = value;
            NotifyPropertyChanged();
            SyncOnTabSwitch(previousTab, value);
        }
    }

    public string ClipXml
    {
        get => _xmlDocument?.Text ?? Clip.XmlData;
        set
        {
            Clip.XmlData = value;
            if (_xmlDocument != null)
            {
                _xmlDocument.Text = value ?? string.Empty;
            }
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(XmlDocument));
        }
    }

    private void SyncOnTabSwitch(int fromTab, int toTab)
    {
        if (_isSyncing || !IsScriptClip) return;
        _isSyncing = true;

        try
        {
            if (toTab == 1 && _xmlDocument != null)
            {
                // Switching to Script tab: XML → HR
                var hr = XmlToHrConverter.Convert(_xmlDocument.Text);
                if (_scriptDocument != null)
                    _scriptDocument.Text = hr;
            }
            else if (fromTab == 1 && _scriptDocument != null)
            {
                // Switching away from Script tab: HR → XML
                var result = HrToXmlConverter.Convert(_scriptDocument.Text);
                Clip.XmlData = result.Xml;
                if (_xmlDocument != null)
                    _xmlDocument.Text = result.Xml;
            }
        }
        catch
        {
            // Conversion failed — leave the other document unchanged
        }
        finally
        {
            _isSyncing = false;
        }
    }
}