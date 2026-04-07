using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using SharpFM.Model;
using SharpFM.Plugin;

namespace SharpFM.Plugin.XmlViewer;

public class XmlViewerPlugin : IPanelPlugin
{
    public string Id => "xml-viewer";
    public string DisplayName => "XML Viewer";
    public string Version => GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    private IPluginHost? _host;
    private XmlViewerViewModel? _viewModel;

    public IReadOnlyList<PluginKeyBinding> KeyBindings { get; } =
        [new PluginKeyBinding("Ctrl+Shift+X", "Toggle XML Viewer", () => { })];

    public IReadOnlyList<PluginMenuAction> MenuActions => [];

    public void Initialize(IPluginHost host)
    {
        _host = host;
        _host.SelectedClipChanged += OnClipChanged;
        _host.ClipContentChanged += OnClipContentChanged;
    }

    public Control CreatePanel()
    {
        _viewModel = new XmlViewerViewModel(_host!, Id);
        _viewModel.RefreshFromHost();
        return new XmlViewerPanel { DataContext = _viewModel };
    }

    private void OnClipChanged(object? sender, ClipData? clip)
    {
        _viewModel?.LoadClip(clip);
    }

    private void OnClipContentChanged(object? sender, ClipContentChangedArgs args)
    {
        if (args.Origin == Id) return; // I caused this, skip
        _viewModel?.LoadClip(args.Clip);
    }

    public void Dispose()
    {
        if (_host is null) return;
        _host.SelectedClipChanged -= OnClipChanged;
        _host.ClipContentChanged -= OnClipContentChanged;
    }
}
