# FileMaker clipboard diagnostic snapshots

Status: implemented; real FileMaker platform validation/captures pending
Related issue: [#186](https://github.com/fuzzzerd/SharpFM/issues/186)

## Goal

Let a FileMaker user on Windows, macOS, or Linux capture clipboard evidence and save a snapshot they can attach to an issue. Preserve the original payloads independently of SharpFM's normal decoding assumptions, so maintainers can diagnose format discovery, data retrieval, encoding, framing, and round-trip failures.

This is a FileMaker support tool, not a general-purpose clipboard editor or a collection of hex/text/XML viewers.

## Current limitation

In v2.0.0, **Tools → Raw Clipboard Viewer**:

- Filters identifiers to `Mac-XM*` and reads only the first matching format.
- Uses the normal clipboard service, which requests a byte representation.
- Calls `Clip.FromWireBytes`, which unconditionally skips four bytes.
- Shows decoded XML and a byte count, but cannot export untouched bytes.

The viewer therefore shares the assumption investigated by #186. It cannot provide an independent payload fixture.

Relevant implementation:

- `src/SharpFM/Diagnostics/RawClipboardWindow.axaml.cs`
- `src/SharpFM/Diagnostics/RawClipboardWindow.axaml`
- `src/SharpFM/Services/ClipboardService.cs`
- `src/SharpFM.Model/Clip.cs`

## Scope

Expand the existing helper into **FileMaker Clipboard Diagnostics** with two primary actions:

1. **Capture FileMaker Clipboard** — read the clipboard and retain an in-memory snapshot.
2. **Save Snapshot…** — export that captured snapshot without rereading the clipboard.

Keep the UI to a capture summary, a simple format/result list, interpretation summaries, and export status. No dedicated payload viewers are required.

Do not change normal FileMaker import/export behavior or introduce a platform-specific prefix fix in this work.

## Capture model

Separate capture, interpretation, and export from the window and from `Clip.FromWireBytes`.

### Environment

Record:

- Snapshot schema version and UTC capture time.
- SharpFM version/build, OS version, process architecture, and Avalonia version.
- Optional user-entered FileMaker version and description of what was copied.
- Capture source explicitly identified as Avalonia's clipboard API.

Do not collect usernames, machine names, unrelated filesystem paths, or other unnecessary identifying details.

### Formats and payloads

- Enumerate every format exposed by Avalonia. Do not filter discovery by `Mac-XM*`; different platform identifiers may be part of the failure.
- Attempt to capture the available payloads from that FileMaker copy, preserving every successfully retrieved byte payload in full.
- Where the API exposes a supported non-byte representation, record its representation/type explicitly; preserve text separately without calling it original wire bytes.
- For each entry, record its identifier, requested/exposed representation where available, retrieval outcome, returned runtime type, payload length, and sanitized error details.
- Keep null, unsupported, and failed retrievals visible. One failing format must not discard successful captures.
- Never strip a prefix, pretty-print XML, normalize line endings, or re-encode a byte payload before storing it.
- Prefer one clipboard transfer/read session per capture, retaining it until retrieval completes. Do not use a fresh clipboard read for each format as the existing service does.
- A capture is best-effort, not a guarantee of an atomic native pasteboard dump. Record limitations and any detected clipboard change.

Audit Avalonia's format/retrieval APIs before implementing the collector. The current `GetDataAsync(string)` forces a byte-format request and is insufficient to describe every exposed representation. Keep any new diagnostic access separate from normal paste behavior.

The snapshot preserves what Avalonia returns. Do not claim it includes native formats or bytes that Avalonia cannot expose. Native platform collectors are a possible follow-up only if real captures demonstrate that gap.

## Non-destructive interpretations

Run interpretations against stored payloads, never against the live clipboard. Retain results separately from original bytes; interpretation failures must not prevent export.

For plausible FileMaker byte payloads, summarize:

- Total byte length and a limited leading-byte hex preview.
- First four bytes interpreted as an unsigned little-endian length, when present.
- Whether that value matches the remaining payload length.
- Strict UTF-8 decoding and XML well-formedness results at offsets **0** and **4**.
- XML root name and snippet type, where parsing succeeds.
- Encoding/BOM observations and concise errors where relevant.

Label offset-0 and offset-4 results as hypotheses, not a detected platform rule. Do not route these probes through `Clip.FromWireBytes` or require domain-model parsing.

Bound interpretation work for large payloads, disable XML external resolution/DTDs, and report skipped probes explicitly. Limits on interpretation must not silently truncate exported raw payloads.

## Shareable snapshot format

Export a ZIP containing:

- `manifest.json`: versioned environment, format/retrieval metadata, interpretation results, and payload references.
- `payloads/`: exact captured byte payloads and explicitly identified supported non-byte representations.

Use generated filenames rather than clipboard identifiers as paths. Record byte lengths and SHA-256 checksums for binary payloads. Include a brief description in the manifest of the capture boundary and any incomplete entries.

A successful export means the saved payloads match the in-memory capture. If export fails, show the error, retain the snapshot for retry, and avoid leaving a misleading completed archive.

The archive must be useful without opening it in SharpFM: maintainers should be able to inspect the JSON and use a binary payload directly as a regression fixture. Importing snapshots into the app is not required initially.

## User experience and privacy

- Explain the workflow: copy a small, non-sensitive example in FileMaker, switch to SharpFM, capture, then save and attach the ZIP to an issue.
- Show capture time, all discovered identifiers, retrieval outcomes, sizes, interpretation summaries, and whether capture was partial.
- Disable Save until a snapshot exists. Export the retained snapshot even if the live clipboard has subsequently changed.
- Warn that the archive contains full clipboard contents, including accompanying text formats, and may contain proprietary scripts, credentials, or personal data.
- Never upload automatically or log payload contents. Do not present a hex preview or summary as automatically safe to share.
- Capture, interpretation, and export must not modify the clipboard. Avoid retaining the existing **Copy XML** action in this capture-focused workflow.
- Keep the UI responsive during capture/export. If a resource limit prevents complete capture, report that entry as incomplete rather than silently exporting truncated bytes.

## Validation

Unit-test capture orchestration using a fake clipboard source, with interpretation and export tests independent of Avalonia windows:

- Multiple identifiers, including unfamiliar/non-`Mac-XM` identifiers.
- Null, unsupported, non-byte, and throwing retrievals alongside successful entries.
- One capture session and export without a clipboard reread.
- Prefixed XML, unprefixed XML, mismatched length headers, empty/short payloads, invalid UTF-8, and malformed XML.
- Safe XML parsing and explicitly skipped oversized interpretations.
- Exact binary preservation, checksums, generated safe filenames, schema versioning, and partial capture metadata.
- Export failure/retry and large-payload behavior without silent truncation.

Manually validate against real FileMaker copies on Windows and macOS, recording FileMaker versions. Validate the Linux capture/export path where an appropriate clipboard source is available; do not imply FileMaker runs natively there.

## Delivery order

1. Define the snapshot schema and diagnostic collector boundary.
2. Implement untouched capture, independent interpretations, and archive export with tests.
3. Replace the current viewer UI with the compact capture/save workflow.
4. Document capture instructions and the privacy warning in user-facing documentation.
5. Collect real Mac and Windows snapshots for #186, then use the evidence to plan any wire-format correction and regression fixtures separately.

## Implementation and validation

- Implemented on `fuzzz/186-filemaker-clipboard-diagnostics`, based on latest `origin/master` at `f09da3e`.
- Replaced the existing helper with a compact capture/save workflow; normal clipboard import/export is unchanged.
- Added a version-1 JSON/ZIP snapshot format, per-item/per-format collection through one Avalonia transfer, exact byte preservation, strict UTF-8 text export, independent interpretations, and atomic local-file export.
- Retention is limited to 128 MiB per capture, with explicit omitted-entry outcomes; XML probes are limited to 2 MiB without truncating retained payloads.
- Added [user instructions, archive details, limitations, and privacy guidance](../filemaker-clipboard-diagnostics.md).
- Added 31 automated tests under `tests/SharpFM.Tests/Diagnostics/` covering capture, interpretations, summaries, and export.
- `dotnet build SharpFM.sln --no-restore` passes (existing unrelated warnings remain).
- `dotnet test SharpFM.sln --no-restore --disable-build-servers -m:1` passes: 2,374 tests across both test projects. A default parallel invocation encountered a transient CLR host abort; two subsequent isolated full-suite runs passed.
- `dotnet format whitespace SharpFM.sln --no-restore --verify-no-changes --include src/SharpFM/Diagnostics tests/SharpFM.Tests/Diagnostics` passes.
- Real FileMaker captures and native UI smoke tests on macOS/Windows are still required; no prefix correction has been inferred or implemented.

### Remaining platform validation

1. On Windows and macOS, copy a small non-sensitive FileMaker example, capture it, save the ZIP, and verify the listed formats and exported byte lengths/checksums.
2. Repeat after copying from SharpFM; label the two archives by direction and test actual pasting into FileMaker separately.
3. Confirm partial failures remain exportable and saving does not replace the live clipboard.
4. On Linux, validate native clipboard capture/save with an appropriate source, without implying FileMaker runs natively there.
5. Attach safe real captures to #186 and derive framing changes/regression fixtures from that evidence.

## Deferred

- Dedicated hex, text, or XML viewers.
- Snapshot comparison UI or in-app snapshot import.
- Arbitrary clipboard editing or payload replay.
- Native platform collectors without demonstrated need.
- Any automatic prefix correction or platform-specific import/export change.
