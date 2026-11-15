# FileMaker clipboard mapping (#186)

Local implementation on `fuzzz/186-cross-platform-filemaker-clipboard`.
No publishing or PR without permission.

## Fix

Keep one explicit bidirectional mapping in `ClipboardService`, using the supplied
FileMaker calculation and the seven downloaded clipboard captures:

| Existing logical type | macOS clipboard key |
| --- | --- |
| `Mac-XMSC` (scripts) | `dyn.ah62d4rv4gk8zuxnxkq` |
| `Mac-XMSS` (script steps) | `dyn.ah62d4rv4gk8zuxnxnq` |
| `Mac-XMTB` (tables) | `dyn.ah62d4rv4gk8zuxnykk` |
| `Mac-XMFD` (fields) | `dyn.ah62d4rv4gk8zuxngku` |
| `Mac-XML2` (layout objects) | `dyn.ah62d4rv4gk8zuxnqgk` |
| `Mac-XMFN` (custom functions) | `dyn.ah62d4rv4gk8zuxngm2` |
| `Mac-XMVL` (value lists; from ZIP) | `dyn.ah62d4rv4gk8zuxn0mu` |

Read actual Avalonia item descriptors: map incoming keys to logical IDs and accept
macOS XML strings directly. Keep the existing prefixed-byte conversion for Windows.
Write current XML as a string under the mapped key on macOS; otherwise keep the
existing logical identifier and Windows wire bytes. Unknown macOS output types fail
through existing error handling. Normal copy and both script-copy commands use this
service; naming, grouping, editors, persistence, plain-text copy, and diagnostics
remain unchanged.

No native interop, UTI resolver, custom decoder/validation policy, warning/result
framework, Problems-panel changes, or additional clipboard infrastructure.

## Checks

All seven captured XML strings are fixtures under
`tests/SharpFM.Tests/Fixtures/FileMakerClipboard/`. Test mappings both ways, Windows
non-ASCII byte lengths, runtime output selection without a prior read, and the three
copy commands' wrapper choices. Full solution: 2,390 tests pass locally.

Still manually validate FileMaker paste/re-copy on Windows and macOS before claiming
platform support. The ZIPs establish inbound strings, not successful outbound paste.
