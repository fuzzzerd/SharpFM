# FileMaker clipboard fixtures

Source: [#186 attachment](https://github.com/fuzzzerd/SharpFM/issues/186#issuecomment-6861883978),
`SharpFM_clipboard_snapshots_macOS_27.2.1_M4_aarch64.zip`.

The seven XML files are verbatim `payloads/0000.txt` exports from the corresponding
nested captures. They are Avalonia strings exported as UTF-8, not native wire bytes.
The filenames identify the types; optional descriptions were one capture behind.
The value-list capture adds `dyn.ah62d4rv4gk8zuxn0mu` -> `Mac-XMVL` to the supplied
FileMaker mapping. Other mappings are listed in the local plan and service tests.

Preserve original bytes/line endings (`.gitattributes`). Accompanying image/PDF
exports are excluded. Custom functions and value lists retain opaque XML handling.
These fixtures do not establish FileMaker output acceptance or editor fidelity.
