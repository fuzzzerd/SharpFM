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

    public PluginConfigSchema ConfigSchema { get; } = new(new[]
    {
        new PluginConfigField(
            Key: "ShowElementCount",
            Label: "Show XML element count",
            Type: PluginConfigFieldType.Bool,
            DefaultValue: true,
            Description: "Display the number of XML elements in the selected clip."),
        new PluginConfigField(
            Key: "ShowXmlSize",
            Label: "Show approximate size",
            Type: PluginConfigFieldType.Bool,
            DefaultValue: true,
            Description: "Display the approximate byte size of the clip's XML."),
    });

    public void OnConfigChanged(IReadOnlyDictionary<string, object?> values)
    {
        _showElementCount = values.TryGetValue("ShowElementCount", out var a) && a is bool ba ? ba : true;
        _showXmlSize = values.TryGetValue("ShowXmlSize", out var b) && b is bool bb ? bb : true;
        if (_viewModel is not null)
        {
            _viewModel.ShowElementCount = _showElementCount;
            _viewModel.ShowXmlSize = _showXmlSize;
        }
    }

    private IPluginHost? _host;
    private ClipInspectorViewModel? _viewModel;
    private bool _showElementCount = true;
    private bool _showXmlSize = true;

    public void Initialize(IPluginHost host)
    {
        _host = host;
        _host.SelectedClipChanged += OnSelectedClipChanged;
        _host.ClipContentChanged += OnClipContentChanged;
    }

    public Control CreatePanel()
    {
        _viewModel = new ClipInspectorViewModel
        {
            ShowElementCount = _showElementCount,
            ShowXmlSize = _showXmlSize,
        };
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
