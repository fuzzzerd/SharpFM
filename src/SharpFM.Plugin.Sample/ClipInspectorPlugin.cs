using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using SharpFM.Model;
using SharpFM.Plugin;
using SharpFM.Plugin.UI;

namespace SharpFM.Plugin.Sample;

public class ClipInspectorPlugin : IPanelPlugin
{
    public string Id => "clip-inspector";
    public string DisplayName => "Clip Inspector";
    public string Description => "Displays clip metadata including name, type, element count, and size.";
    public string Version => GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
    public IReadOnlyList<PluginKeyBinding> KeyBindings => [];
    public IReadOnlyList<PluginMenuAction> MenuActions => [];

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

    private void OnSelectedClipChanged(object? sender, ClipData? clip)
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
