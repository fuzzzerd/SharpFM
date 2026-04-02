# SharpFM

SharpFM is a cross-platform (Mac, Windows, Linux) FileMaker Pro Developer Utility to help when migrating FileMaker code between FileMaker files, sharing FileMaker code with other developers, or simply see what's happening under the hood. This means you can copy scripts, layouts, tables, etc (anything you can copy/paste in FileMaker) across machine barriers like Remote Desktop, Citrix/XenApp or anything that can copy plain text between machines, save them for later, change them if you want, then finally re-paste them into the same or other FileMaker file.

## Getting Started

Note: SharpFM and FileMaker must be running on the same computer. In order to share or move a clip across machine barriers, you must share the text based XML version.

- Head over to [Releases](https://github.com/fuzzzerd/SharpFM/releases), grab the latest version (binaries for Windows, Mac, Linux are all available there).

### Importing Clips from FileMaker

- Open SharpFM.
- Switch over to FileMaker.
- Copy something to the clipboard (scripts, tables, layouts, etc).
- Switch back to SharpFM.
- Use **File > New > From Clipboard** (`Ctrl+V`) to import the clip.
- The clip appears in the left panel with the appropriate editor on the right.

### Exporting Clips to FileMaker

- Select a clip in the left panel.
- Use **File > Save > Selected clip to Clipboard** (`Ctrl+Shift+C`).
- Switch to FileMaker and open the appropriate destination (Database Manager, Script Workspace, Layout mode, etc).
- Paste as you normally would.

### Editing Scripts

- Select a script clip or create one with **File > New > Script** (`Ctrl+N`).
- The script editor shows a plain-text representation of the script steps with FmScript syntax highlighting.
- Edit the script text directly; changes are synced back to the underlying XML.

### Editing Tables

- Select a table clip or create one with **File > New > Table** (`Ctrl+Shift+N`).
- The table editor shows a DataGrid with columns for Field Name, Type, Kind, Required, Unique, and Comment.
- Click **+ Add Field** to add a new field, then edit its properties inline.
- Select a field and click **Remove** or press `Delete` to remove it.
- Change a field's Kind to Calculated or Summary, then click **Edit Calculation...** to open the calculation editor.

### Viewing Raw XML

- Select any clip and use **View > Show XML** (`Ctrl+Shift+X`) to open the raw XML in a separate window.
- Edits made in the XML window are synced back to the clip when the window is closed.

### Saving and Sharing Clips

SharpFM persists clips between sessions as XML files in a local folder.

- Use **File > Save > Save All To Folder** (`Ctrl+S`) to save all clips.
- Use **File > Open Folder** to load clips from a different folder.
- The clip files are plain XML and can be shared via git, email, or any text-based tool.

## Features

- [x] Copy FileMaker Scripts, Tables, or Layouts From FileMaker Pro to their XML representation and back into FileMaker.
- [x] Store FileMaker Scripts, Tables, and Layouts to xml files that can be shared via git, email or other text based tools.
- [x] Edit raw FileMaker XML code (scripts, layouts, tables) with ability to paste changes back into FileMaker.
- [x] Use AvaloniaEdit for XML editing with XML syntax highlighting.
- [x] Plain-text script editor with FmScript syntax highlighting.
- [x] DataGrid table/field editor with inline editing, calculation editor, and type/kind selection.
- [x] View and edit raw XML alongside structured editors.

## Plugins

SharpFM supports plugins via the `SharpFM.Plugin` contract library. Plugins implement `IPanelPlugin` and are loaded from the `plugins/` directory at startup. You can also install and manage plugins from the **View > Manage Plugins...** menu.

A sample "Clip Inspector" plugin is included to demonstrate the plugin API.

### Writing a Plugin

1. Create a new .NET 8 class library referencing `SharpFM.Plugin`.
2. Implement `IPanelPlugin` — provide an `Id`, `DisplayName`, and `CreatePanel()` returning an Avalonia `Control`.
3. Use `IPluginHost` in `Initialize()` to observe clip selection and push XML updates.
4. Build your DLL and drop it in the `plugins/` directory.

See `src/SharpFM.Plugin.Sample/` for a complete working example.

### License

While SharpFM is licensed under GPL v3, plugins that communicate solely through the interfaces in `SharpFM.Plugin` are not required to be GPL-licensed. See the plugin interface source files for the full exception clause.

## Troubleshooting

Logs are stored in `${specialfolder:folder=CommonApplicationData}\SharpFM` and are automatically rotated after thirty days.

## Similar Mac OS / Apple Based Developer Utilities

- Apple Script utility: <https://github.com/DanShockley/FmClipTools>
- FileMaker based Generator: <https://github.com/proofgeist/generator>

## App Icon

![Sharp FM](Assets/noun-sharp-teeth-monster-4226695.small.png)

sharp teeth monster by Kanyanee Watanajitkasem from [Noun Project](https://thenounproject.com/browse/icons/term/sharp-teeth-monster/) (CC BY 3.0)
