# Persistence Plugins

Persistence plugins provide alternative storage backends for clips.

## Interfaces

### IPersistencePlugin

```csharp
public interface IPersistencePlugin : IPlugin
{
    IClipRepository CreateRepository();
}
```

The plugin handles discovery and lifecycle. `CreateRepository()` is called when the user selects this storage provider.

### IClipRepository

```csharp
public interface IClipRepository
{
    string ProviderName { get; }
    string CurrentLocation { get; }
    bool SupportsLocationPicker { get; }
    Task<IReadOnlyList<ClipData>> LoadClipsAsync();
    Task SaveClipsAsync(IReadOnlyList<ClipData> clips);
    Task<string?> PickLocationAsync();
}
```

The repository handles all data operations. Methods are async to support remote backends.

### ClipData

```csharp
public record ClipData(string Name, string ClipType, string Xml);
```

The persistence DTO. Separate from `ClipInfo` (which is used for plugin notifications) so the two can evolve independently.

## Built-In vs Plugin Storage

The built-in file system storage (`ClipRepository`) implements `IClipRepository` directly — it is **not** a plugin. Plugin-provided backends come through `IPersistencePlugin`.

At startup, the host builds a list of available repositories:
1. The built-in file system repository
2. One from each loaded `IPersistencePlugin` via `CreateRepository()`

## Example: Cloud API Storage Plugin

```csharp
public class CloudStoragePlugin : IPersistencePlugin
{
    public string Id => "cloud-storage";
    public string DisplayName => "Cloud Storage";
    public string Version => "1.0.0";
    public IReadOnlyList<PluginKeyBinding> KeyBindings => [];
    public IReadOnlyList<PluginMenuAction> MenuActions => [];

    public void Initialize(IPluginHost host) { }
    public IClipRepository CreateRepository() => new CloudRepository();
    public void Dispose() { }
}

public class CloudRepository : IClipRepository
{
    public string ProviderName => "Cloud API";
    public string CurrentLocation => "https://api.example.com/clips";
    public bool SupportsLocationPicker => false;

    public async Task<IReadOnlyList<ClipData>> LoadClipsAsync()
    {
        // Fetch clips from your API
        return [];
    }

    public async Task SaveClipsAsync(IReadOnlyList<ClipData> clips)
    {
        // Push clips to your API
    }

    public Task<string?> PickLocationAsync() => Task.FromResult<string?>(null);
}
```
