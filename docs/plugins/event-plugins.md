# Event Plugins

Event plugins are headless — they react to host events with no UI panel.

## Interface

```csharp
public interface IEventPlugin : IPlugin { }
```

`IEventPlugin` is a marker interface. All behavior comes from subscribing to `IPluginHost` events in `Initialize()` and unsubscribing in `Dispose()`.

## When to Use

- Auto-formatters that reformat XML on clip selection
- Linters that validate clip structure and post diagnostics
- Sync agents that mirror clips to an external system
- Analytics or telemetry

## Available Events

| Event | When |
|-------|------|
| `SelectedClipChanged` | User selects a different clip |
| `ClipContentChanged` | Clip XML changes (user edit or plugin push) |
| `ClipCollectionChanged` | Clips added, removed, or reloaded |

## Host Capabilities

- `AllClips` — read the full clip collection for bulk operations
- `ShowStatus(message)` — display feedback in the status bar
- `UpdateSelectedClipXml(xml, pluginId)` — push XML changes back to the editor

## Example: Auto-Formatter

```csharp
public class AutoFormatPlugin : IEventPlugin
{
    public string Id => "auto-format";
    public string DisplayName => "Auto Formatter";
    public string Version => "1.0.0";
    public IReadOnlyList<PluginKeyBinding> KeyBindings => [];
    public IReadOnlyList<PluginMenuAction> MenuActions => [];

    private IPluginHost? _host;

    public void Initialize(IPluginHost host)
    {
        _host = host;
        _host.SelectedClipChanged += OnClipChanged;
    }

    private void OnClipChanged(object? sender, ClipInfo? clip)
    {
        if (clip is null) return;
        var formatted = FormatXml(clip.Xml);
        if (formatted != clip.Xml)
        {
            _host!.UpdateSelectedClipXml(formatted, Id);
            _host.ShowStatus("Clip auto-formatted");
        }
    }

    private static string FormatXml(string xml) => xml; // your formatting logic

    public void Dispose()
    {
        if (_host is not null)
            _host.SelectedClipChanged -= OnClipChanged;
    }
}
```
