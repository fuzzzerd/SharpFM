# FileMaker clipboard diagnostic snapshots

Use **Tools > FileMaker Clipboard Diagnostics…** when copying between FileMaker and SharpFM fails, or when maintainers need a real clipboard payload from your platform. The helper works independently of normal FileMaker decoding: it does not strip a length prefix or modify the clipboard.

## Capture and share

1. In FileMaker, copy a **small, non-sensitive example**: a script, script steps, table, fields, or layout.
2. In SharpFM, open **Tools > FileMaker Clipboard Diagnostics…**.
3. Optionally enter your FileMaker version and describe what you copied. These fields are saved in the snapshot.
4. Choose **Capture FileMaker Clipboard**. Review the discovered formats, retrieval outcomes, and interpretation summaries. Select an entry to see its details.
5. Choose **Save Snapshot…** and select a local ZIP destination. Saving exports the retained capture, not a fresh clipboard read.
6. Attach the ZIP to the issue only if its contents are safe to share. Include the copy/paste direction, what happened, and what you expected.

A partial capture is still useful. Save it even if some formats return null, are unsupported, or fail to read. The ZIP includes successful payloads and failure metadata. A capture with no payloads can still explain a discovery/access failure.

For a two-direction investigation, save separate snapshots immediately after copying in FileMaker and after using SharpFM's normal **Copy to FileMaker** command. Describe which is which. The diagnostic helper does not replay or write custom clipboard payloads itself.

## Privacy

**Snapshots contain full clipboard data, not just diagnostic metadata.** They capture all formats Avalonia exposes from that copy, including accompanying text and unfamiliar identifiers. They may contain proprietary scripts, credentials, personal information, or other sensitive content. The optional description, XML root/type observations, and leading-byte previews may also contain private information.

- Use a newly created, non-sensitive FileMaker example where possible.
- Capturing does not upload anything. Saving writes a local ZIP; attaching it to an issue is your choice.
- Payloads are not written to application logs or persisted automatically.
- The ZIP is not encrypted. Protect it like the original data. On Unix systems, newly exported archives are created with owner-only read/write permissions.
- Inspect `manifest.json` and the files under `payloads/` locally if necessary. Summaries are not a redacted substitute for reviewing the full capture.
- Closing the helper discards its in-memory capture. Delete exported snapshots when no longer needed.

## What is captured

The manifest records:

- Schema version, UTC capture time, SharpFM build version, OS version, process architecture, and Avalonia version.
- Optional FileMaker version and description.
- Every format identifier exposed by the transfer and its exposed representation/kind.
- Per-item/per-format retrieval outcomes, returned runtime types, sizes, sanitized errors, payload references, and SHA-256 checksums.
- Independent interpretation results and explicit capture limitations.

Every successfully retained byte array is exported **exactly as returned by Avalonia**. Supported text values are encoded as strict UTF-8 without a BOM and explicitly labeled as text representations, not original wire bytes. There is no XML formatting, prefix removal, newline normalization, arbitrary object serialization, or dereferencing of clipboard file objects.

One transfer is held open while its items are read, then disposed. This is a best-effort capture through Avalonia, **not a complete native macOS pasteboard/Windows clipboard dump**. Avalonia may expose converted representations or omit native formats. Atomicity and clipboard-change detection are not guaranteed. Unknown object representations are reported by type but not persisted.

The retention budget is 128 MiB per capture. Entries exceeding the remaining budget are marked `ResourceLimit`, with their observed size and no payload file; they are not silently truncated. Smaller subsequent entries can still be retained. The platform API may allocate data before the helper can check its size, so use small examples.

## Interpretation summaries

For retained payloads, the helper reports:

- Up to 32 leading bytes as hex and a BOM observation.
- The first four bytes as an unsigned little-endian length and whether that matches the remaining byte count.
- Strict UTF-8 decoding and XML well-formedness probes starting at offsets **0** and **4**, with BOM observations at each offset.
- XML root name and `fmxmlsnippet` type if parsing succeeds. Long values are abbreviated only in the summary.

These are hypotheses, not a platform-specific decoding decision. XML probes are limited to 2 MiB each; larger probes are explicitly skipped while retained payloads remain complete. DTDs and external XML resolution are disabled. Errors omit exception messages to avoid embedding clipboard contents or local paths.

For issue [#186](https://github.com/fuzzzerd/SharpFM/issues/186), a matching length plus valid XML at offset 4 supports the prefixed-payload hypothesis. Valid XML at offset 0 supports the unprefixed hypothesis. Neither replaces checking the real bytes and testing both copy/paste directions in FileMaker.

## Archive layout (schema version 1)

```text
manifest.json
payloads/0000.bin
payloads/0001.txt
...
```

`manifest.json` uses camel-case property names and string retrieval outcomes. Each entry includes its zero-based `itemIndex`, `format`, `outcome`, and, when retained, `payloadFile`, `payloadRepresentation`, `byteLength`, and `sha256`. Payload filenames are generated independently of format identifiers. Missing or unsupported values have no payload file. `isPartial`, `issues`, `captureBoundary`, and `limits` describe omissions and collection constraints.

No ZIP import or special viewer is required: maintainers can read the JSON and use a `.bin` file directly as a regression fixture. Export writes a temporary archive beside the destination and replaces the destination only after the archive is complete. If saving fails, the in-memory capture remains available for retry.
