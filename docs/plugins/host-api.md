# IPluginHost API Reference

The `IPluginHost` interface is provided to plugins during `Initialize()`. It exposes the host application's state and services.

## Properties

### `ClipInfo? SelectedClip`

The currently selected clip, or `null` if nothing is selected. Returns a snapshot — reading it multiple times may return different instances if the clip changed.

### `IReadOnlyList<ClipInfo> AllClips`

All clips currently loaded in the application. Useful for plugins that operate across the full clip set (linters, search indexers, sync agents).

## Events

### `SelectedClipChanged`

```csharp
event EventHandler<ClipInfo?> SelectedClipChanged;
```

Raised when the user selects a different clip in the list. The argument is the new clip, or `null` if deselected.

### `ClipContentChanged`

```csharp
event EventHandler<ClipContentChangedArgs> ClipContentChanged;
```

Raised when clip content changes. Check `args.Origin` to determine the source:
- `"editor"` — user edited in the structured editor (debounced)
- Plugin ID — a plugin pushed XML changes (immediate)

```csharp
public record ClipContentChangedArgs(ClipInfo Clip, string Origin, bool IsPartial);
```

`IsPartial` is `true` when the XML was produced from an incomplete parse (e.g., user is mid-edit in the script editor).

### `ClipCollectionChanged`

```csharp
event EventHandler? ClipCollectionChanged;
```

Raised when clips are added, removed, or the collection is reloaded.

## Methods

### `UpdateSelectedClipXml(string xml, string originPluginId)`

Replace the XML content of the currently selected clip. The host syncs the new XML back to the structured editor automatically. Pass your plugin's `Id` as `originPluginId` for origin tagging — you'll receive your own change back via `ClipContentChanged` but can skip it by checking `args.Origin == Id`.

### `RefreshSelectedClip() -> ClipInfo?`

Flush the editor's in-progress state to XML and return a fresh snapshot. Use this before reading `SelectedClip` if you need XML that reflects any uncommitted edits in the structured editors.

### `ShowStatus(string message)`

Display a message in the status bar. Use this for user-visible feedback. The message auto-clears after a few seconds.

## ClipInfo

```csharp
public record ClipInfo(string Name, string ClipType, string Xml);
```

A read-only snapshot of clip metadata and content. `ClipType` is the FileMaker clipboard format (e.g., `"Mac-XMSS"` for script steps, `"Mac-XMTB"` for tables).
