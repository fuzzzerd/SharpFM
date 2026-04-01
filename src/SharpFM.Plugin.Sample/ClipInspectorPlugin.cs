using System;
using System.Collections.Generic;
using Avalonia.Controls;
using SharpFM.Plugin;

namespace SharpFM.Plugin.Sample;

public class ClipInspectorPlugin : IPanelPlugin
{
    public string Id => "clip-inspector";
    public string DisplayName => "Clip Inspector";
    public IReadOnlyList<PluginKeyBinding> KeyBindings => [];

    private IPluginHost? _host;
    private ClipInspectorViewModel? _viewModel;

    public void Initialize(IPluginHost host)
    {
        _host = host;
        _host.SelectedClipChanged += OnSelectedClipChanged;
        _host.ClipContentChanged += OnClipContentChanged;
    }

    public Control CreatePanel()
    {
        _viewModel = new ClipInspectorViewModel();
        _viewModel.Update(_host?.SelectedClip);
        return new ClipInspectorPanel { DataContext = _viewModel };
    }

    private void OnSelectedClipChanged(object? sender, ClipInfo? clip)
    {
        _viewModel?.Update(clip);
    }

    private void OnClipContentChanged(object? sender, ClipContentChangedArgs args)
    {
        _viewModel?.Update(args.Clip);
    }

    public void Dispose()
    {
        if (_host is null) return;
        _host.SelectedClipChanged -= OnSelectedClipChanged;
        _host.ClipContentChanged -= OnClipContentChanged;
    }
}
