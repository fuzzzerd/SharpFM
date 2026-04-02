# Panel Plugins

Panel plugins provide a sidebar UI panel in the SharpFM main window.

## Interface

```csharp
public interface IPanelPlugin : IPlugin
{
    Control CreatePanel();
}
```

`CreatePanel()` is called once when the user activates the plugin. Only one panel plugin can be active at a time. Toggling the same plugin closes it; toggling a different one switches the panel content.

## Lifecycle

1. `Initialize(IPluginHost host)` — subscribe to host events here
2. `CreatePanel()` — create your Avalonia `Control` (called when plugin is toggled on)
3. `Dispose()` — unsubscribe from events and clean up

## Subscribing to Events

```csharp
public void Initialize(IPluginHost host)
{
    _host = host;
    _host.SelectedClipChanged += OnClipChanged;
    _host.ClipContentChanged += OnContentChanged;
}
```

## Bidirectional Sync

If your plugin edits clip XML, use origin tagging to avoid feedback loops:

```csharp
// Push changes to the host
_host.UpdateSelectedClipXml(newXml, Id);

// Skip your own updates
private void OnContentChanged(object? sender, ClipContentChangedArgs args)
{
    if (args.Origin == Id) return; // I caused this change
    _viewModel.LoadClip(args.Clip);
}
```

## Example: Minimal Read-Only Panel

```csharp
public class MyPlugin : IPanelPlugin
{
    public string Id => "my-plugin";
    public string DisplayName => "My Plugin";
    public string Version => "1.0.0";
    public IReadOnlyList<PluginKeyBinding> KeyBindings => [];
    public IReadOnlyList<PluginMenuAction> MenuActions => [];

    private IPluginHost? _host;

    public void Initialize(IPluginHost host) => _host = host;

    public Control CreatePanel()
    {
        var text = new TextBlock { Text = _host?.SelectedClip?.Name ?? "No clip" };
        _host!.SelectedClipChanged += (_, clip) => text.Text = clip?.Name ?? "No clip";
        return text;
    }

    public void Dispose() { }
}
```
