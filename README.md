# SharpFM

**Get FileMaker objects out of FileMaker, edit or share them, and paste them back.**

SharpFM is a cross-platform (Windows, macOS, Linux) developer utility for FileMaker Pro. It captures the proprietary clipboard format FileMaker uses for scripts, tables, fields, and layouts, exposes it as plain XML plus structured editors, and lets you paste the result back into FileMaker when you're done.

If you've ever wanted to diff two scripts, commit a table definition to git, or move code across a Remote Desktop / Citrix boundary that strips custom clipboard formats — that's what SharpFM is for.

## Why SharpFM

FileMaker's clipboard is a closed format. Scripts, tables, and layouts never really leave FileMaker — the only way out is a copy/paste into another FileMaker session on the same machine. SharpFM sits in the middle of that copy/paste: it decodes the clipboard into plain XML, gives you real editors to work with it, and re-encodes it on the way back. Whatever you do in between — edit, save, diff, share — is up to you.

That middle step unlocks a few things:

- **Version control FileMaker code.** Save clips as XML files, drop them in a git repo, review diffs like any other source.
- **Share snippets with other developers.** XML files travel through any text-based channel — chat, gists, shared folders.
- **Cross Remote Desktop / Citrix boundaries.** Those environments often strip custom clipboard data. Plain text survives.
- **Edit in a real editor.** Scripts get syntax highlighting, autocomplete, and validation. Tables get a spreadsheet-style grid with a calculation editor.
- **Inspect what FileMaker is actually doing.** See the raw XML behind any clip and learn the underlying structure.

## How It Works

1. **Copy from FileMaker.** Select a script, table, field set, or layout in FileMaker and copy it.
2. **Paste into SharpFM** (`Ctrl+V` / `Edit > Paste from FileMaker`). The clip appears in the tree on the left; the matching editor opens on the right.
3. **Edit or save.** Modify the script text, tweak field definitions, or save the clip as an XML file to share or commit.
4. **Copy back to FileMaker** (`Ctrl+Shift+C` / `Edit > Copy to FileMaker`), then paste into the Script Workspace, Database Manager, or Layout mode.

SharpFM and FileMaker need to run on the same machine for clipboard hand-off. To move clips between machines, share the XML files directly.

## Getting Started

Grab the latest build for your platform from [Releases](https://github.com/fuzzzerd/SharpFM/releases).

## Features

- **Round-trip clipboard support** for FileMaker scripts, tables, fields, and layouts.
- **FmScript editor** with syntax highlighting, autocomplete, bracket matching, and inline validation diagnostics.
- **Table/field grid editor** with inline editing, type and kind selection, and a dedicated calculation editor for calculated and summary fields.
- **Live XML view** (`Ctrl+Shift+X`) — structured edits and raw XML stay in sync, either direction.
- **New clips from scratch** — start an empty script (`Ctrl+N`) or table (`Ctrl+Shift+N`) without needing to copy from FileMaker first.
- **Tree browser with VS Code-style tabs** for working across multiple clips at once.
- **Plain-XML storage** — clips are files on disk, shareable by any text-based channel.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+V` | Paste from FileMaker |
| `Ctrl+Shift+C` | Copy to FileMaker |
| `Ctrl+N` | New Script |
| `Ctrl+Shift+N` | New Table |
| `Ctrl+S` | Save All |
| `Ctrl+Shift+X` | Toggle XML Viewer |

## Troubleshooting

Logs are stored in `${specialfolder:folder=CommonApplicationData}\SharpFM` and rotate after thirty days.

## Similar Tools

- [FmClipTools](https://github.com/DanShockley/FmClipTools) — AppleScript-based clipboard utilities (macOS only).
- [Generator](https://github.com/proofgeist/generator) — FileMaker-based code generator.

## App Icon

![Sharp FM](src/SharpFM/Assets/noun-sharp-teeth-monster-4226695.small.png)

sharp teeth monster by Kanyanee Watanajitkasem from [Noun Project](https://thenounproject.com/browse/icons/term/sharp-teeth-monster/) (CC BY 3.0)
