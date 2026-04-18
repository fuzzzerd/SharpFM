# SharpFM

SharpFM is a cross-platform (Mac, Windows, Linux) FileMaker Pro Developer Utility for migrating FileMaker code between files, sharing code with other developers, or inspecting what's happening under the hood. Copy scripts, layouts, tables, and fields from FileMaker Pro, edit them in structured editors, and paste them back -- across machine barriers like Remote Desktop, Citrix, or anything that supports plain text.

## Getting Started

Head over to [Releases](https://github.com/fuzzzerd/SharpFM/releases) and grab the latest version for your platform (Windows, Mac, Linux).

SharpFM and FileMaker must be running on the same computer. To share clips across machines, use the XML files directly.

### Importing Clips from FileMaker

1. Open SharpFM and FileMaker side by side.
2. In FileMaker, copy something to the clipboard (scripts, tables, layouts, etc).
3. In SharpFM, use **Edit > Paste from FileMaker** (`Ctrl+V`).
4. The clip appears in the left panel with the appropriate editor on the right.

### Exporting Clips to FileMaker

1. Select a clip in the left panel.
2. Use **Edit > Copy to FileMaker** (`Ctrl+Shift+C`).
3. Switch to FileMaker and open the appropriate destination (Database Manager, Script Workspace, Layout mode, etc).
4. Paste as you normally would.

### Creating New Clips

- **File > New Script** (`Ctrl+N`) -- creates an empty script clip with the FmScript editor.
- **File > New Table** (`Ctrl+Shift+N`) -- creates an empty table clip with the DataGrid editor.

### Editing Scripts

Select a script clip to open the plain-text script editor with FmScript syntax highlighting, autocomplete, bracket matching, and validation diagnostics. Edit the script text directly -- changes sync to the underlying XML automatically.

### Editing Tables

Select a table clip to open the DataGrid editor with columns for Field Name, Type, Kind, Required, Unique, and Comment. Use **+ Add Field** to add fields, **Remove** or `Delete` to remove them. Change a field's Kind to Calculated or Summary, then click **Edit Calculation...** for the calculation editor.

### Viewing Raw XML

Use the **XML Viewer** plugin (`Ctrl+Shift+X`) to open a live XML panel alongside any structured editor. Edits in either direction sync automatically -- change the script and the XML updates, edit the XML and the script rebuilds.

### Saving and Loading Clips

SharpFM persists clips as XML files in a local folder.

- **File > Save All** (`Ctrl+S`) -- saves all clips to the current folder.
- **File > Open Folder...** -- load clips from a different folder.
- Clip files are plain XML and can be shared via git, email, or any text-based tool.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New Script |
| `Ctrl+Shift+N` | New Table |
| `Ctrl+V` | Paste from FileMaker |
| `Ctrl+Shift+C` | Copy to FileMaker |
| `Ctrl+S` | Save All |
| `Ctrl+Shift+X` | Toggle XML Viewer |

## Menu Reference

| Menu | Items |
|------|-------|
| **File** | New Script, New Table, Open Folder..., Save All, Exit |
| **Edit** | Paste from FileMaker, Copy to FileMaker, Copy as C# Class |
| **Plugins** | Loaded plugins (toggle panels), Manage Plugins... |

## Features

- Copy FileMaker Scripts, Tables, Fields, and Layouts to their XML representation and back.
- Persist clips as XML files shareable via git, email, or other text-based tools.
- Plain-text script editor with FmScript syntax highlighting, autocomplete, and validation.
- DataGrid table/field editor with inline editing, calculation editor, and type/kind selection.
- Live bidirectional XML viewer -- edit XML or structured data, both stay in sync.
- Extensible plugin architecture for adding custom panels and tools.

## Plugins

SharpFM supports plugins via the `SharpFM.Plugin` contract library. Plugins are loaded from the `plugins/` directory at startup and can be managed from **Plugins > Manage Plugins...**.

### Bundled Plugins

- **XML Viewer** -- Live XML panel with syntax highlighting and bidirectional sync (`Ctrl+Shift+X`).
- **Clip Inspector** -- Displays clip metadata (name, type, element count, size).

### Writing a Plugin

1. Create a .NET 10 class library referencing `SharpFM.Plugin`.
2. Implement `IPanelPlugin` -- provide an `Id`, `DisplayName`, `CreatePanel()` returning an Avalonia `Control`.
3. Use `IPluginHost` in `Initialize()` to observe clip selection changes and content updates.
4. Optionally register keyboard shortcuts via `KeyBindings` and custom menu actions via `MenuActions`.
5. Build the DLL and drop it in the `plugins/` directory.

See `src/SharpFM.Plugin.Sample/` for a complete working example.

## Troubleshooting

Logs are stored in `${specialfolder:folder=CommonApplicationData}\SharpFM` and are automatically rotated after thirty days.

## Similar Mac OS / Apple Based Developer Utilities

- Apple Script utility: <https://github.com/DanShockley/FmClipTools>
- FileMaker based Generator: <https://github.com/proofgeist/generator>

## App Icon

![Sharp FM](src/SharpFM/Assets/noun-sharp-teeth-monster-4226695.small.png)

sharp teeth monster by Kanyanee Watanajitkasem from [Noun Project](https://thenounproject.com/browse/icons/term/sharp-teeth-monster/) (CC BY 3.0)
