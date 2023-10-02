# SharpFM

SharpFM is a cross-platform (Mac, Windows, Linux) FileMaker Pro Developer Utility to help when migrating FileMaker code between FileMaker files, sharing FileMaker code with other developers, or simply see what's happening under the hood. This means you can copy scripts, layouts, tables, etc (anything you can copy/paste in FileMaker) across machine barriers like Remote Desktop, Citrix/XenApp or anything that can copy plain text between machines, save them for later, change them if you want, then finally re-paste them into the same or other FileMaker file.

## Getting Started

Note: SharpFM and FileMaker must be running on the same computer. In order to share or move a clip across machine barriers, you must share the text based XML version.

- Head over to [Releases](https://github.com/fuzzzerd/SharpFM/releases)

### Clipping from FileMaker

- Download the latest version (or a past version if you need)
- Run the application.
- Switch over to FileMaker.
- Copy something to the clipboard.
- Switch back to SharpFM.
- Use the Edit menu to "Paste from FileMaker Blob".
- See your object(s) in the clips list with the Xml editor on the side.

### Clipping from SharpFM to FileMaker

- Ensure you have a clip in SharpFM
- Select the clip in the list
- Use the Edit menu to "Copy As FileMaker Blob"
- Switch to FileMaker: based on the clip type, open Database manger, Script manager, layout mode.
- Paste into FileMaker as you normally would.

### Saving / Sharing XML Clips

This is an area we can improve, with interoperability with some other similar tools. More to come? Contributions welcome.

SharpFM has the option to persist clips between sessions by using the File menu to "Save to Db".

- Save the XML for a given clip as a separate file (copy/paste to Notepad, Nano, email body, etc)
- Share the resulting XML file.
- Use the File menu to create a New clip.
- Select the appropriate clip type (Table, Script, Layout, etc)
- Paste the raw XML into the code editor.

## Features

- [x] Copy FileMaker Scripts, Tables, or Layouts From FileMaker Pro to their XML representation.
- [x] Edit raw FileMaker XML code (scripts, layouts, tables) with ability to paste changes back into FileMaker.
- [x] Use AvaloniaEdit for XML editing with syntax highlighting.
- [x] Persist FileMaker clips between SharpFM runs.
- [ ] Better UI tools to mutate the Raw XML.

## Similar Mac OS / Apple Based Developer Utilities

- Apple Script utility: https://github.com/DanShockley/FmClipTools
- FileMaker based Generator: https://github.com/proofgeist/generator

## App Icon

![Sharp FM](SharpFM.App/Assets/noun-sharp-teeth-monster-4226695.small.png)

sharp teeth monster by Kanyanee Watanajitkasem from [Noun Project](https://thenounproject.com/browse/icons/term/sharp-teeth-monster/) (CC BY 3.0)
