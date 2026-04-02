# Transform Plugins

Transform plugins modify clip XML during import (paste/load) and export (copy to clipboard) operations.

## Interface

```csharp
public interface IClipTransformPlugin : IPlugin
{
    Task<string> OnImportAsync(string clipType, string xml);
    Task<string> OnExportAsync(string clipType, string xml);
    bool IsEnabled { get; set; }
}
```

## When to Use

- Strip internal FileMaker IDs when pasting clips between files
- Inject boilerplate fields into table definitions
- Rename table/field references during refactoring
- Sanitize clips before sharing with other developers
- Convert between FileMaker versions

## Behavior

- **Import**: Runs when clips are pasted from FileMaker or loaded from storage
- **Export**: Runs when clips are copied to the FileMaker clipboard
- **Ordering**: Transforms run in plugin load order (DLL discovery order)
- **Passthrough**: Return the input `xml` unchanged to skip transformation
- **IsEnabled**: Users can toggle transforms on/off without uninstalling

## Example: Strip Internal IDs

```csharp
public class StripIdsPlugin : IClipTransformPlugin
{
    public string Id => "strip-ids";
    public string DisplayName => "Strip Internal IDs";
    public string Version => "1.0.0";
    public bool IsEnabled { get; set; } = true;
    public IReadOnlyList<PluginKeyBinding> KeyBindings => [];
    public IReadOnlyList<PluginMenuAction> MenuActions => [];

    public void Initialize(IPluginHost host) { }

    public Task<string> OnImportAsync(string clipType, string xml)
    {
        if (!IsEnabled) return Task.FromResult(xml);

        // Strip id="..." attributes from elements
        var cleaned = Regex.Replace(xml, @"\s+id=""\d+""", "");
        return Task.FromResult(cleaned);
    }

    public Task<string> OnExportAsync(string clipType, string xml)
    {
        // No transformation on export
        return Task.FromResult(xml);
    }

    public void Dispose() { }
}
```
