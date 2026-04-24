# Advanced FileMaker Scripting Syntax

Status: authoritative reference for all script-step POCOs.

## Purpose

SharpFM renders FileMaker script XML as editable display text. Some XML
state doesn't naturally appear in FileMaker Pro's own display (object
IDs, flags, embedded newlines, etc.). To round-trip that state faithfully
through display-text editing, SharpFM extends FM Pro's display grammar
with a small set of named conventions — collectively, **Advanced
FileMaker Scripting Syntax**.

The extensions are safe because **FileMaker Pro never consumes SharpFM's
display text**. FM Pro reads only the binary `Mac-XMSS` clipboard
payload; `POCO.ToXml()` always emits pure FM Pro XML regardless of
whatever extensions are present in the display. The display text is a
SharpFM-internal surface for user editing, and our display parser only
ever reads our own extended output.

## Core invariant

> `ToDisplayLine()` and `FromDisplayParams()` together are lossless for
> every piece of state the POCO carries. XML state that is dropped is
> dropped because it carries no information, not because the display
> can't express it.

Every POCO's docstring contains a **zero-loss audit** (see below) that
enumerates XML state explicitly and marks each item as either rendered
by FM Pro natively, covered by an advanced-syntax extension, or
intentionally dropped with a written rationale.

## The three extension forms

Pick the form that matches the shape of the hidden state:

| Shape of state | Syntax | Example | Used for |
|---|---|---|---|
| Id annotation on a named reference | `(#id)` suffix after the name | `"Find Customer" (#7)` | Script, Layout, TableOccurrence, Field id round-trip |
| Boolean / enum flag already in FM Pro's grammar | word token inline, matching FM Pro's wording | `Exit after last: On` | Flags FM Pro itself renders; we mirror its wording |
| Bulk invisible state (multi-slot / structured) | trailing `; Kind: [...]` block, word-token values | `; Buttons: ["OK" commit; "Cancel" nocommit]` | Button configurations, input-field metadata, any future bulk state |

### Form 1 — `(#id)` suffix

Appended to a quoted or `Table::Field` name:

- `FieldRef` emits `People::FirstName (#7)` when an id is known; plain
  `People::FirstName` otherwise.
- `PerformScriptStep` emits `"Find Customer" (#42)`.
- `GoToLayoutStep` emits `"Invoices Detail" (#11)`.

Omitted when the id is zero or unknown — `(#0)` would be visual noise
for unresolved references.

### Form 2 — inline word tokens

Parsed at a specific named prefix, matching FM Pro's own rendering of
the same flag:

- `Exit after last: On` on found-set iterators.
- `With dialog: Off` on steps that can suppress their confirmation UI.
- `Restore: On|Off` — **reserved**. Not currently emitted by any POCO;
  see "What to drop vs. surface" below for the rationale.

### Form 3 — trailing `; Kind: [...]` blocks

Bulk or structured state that doesn't reduce to a single flag:

- `; Buttons: ["OK" commit; "Cancel" nocommit; "" nocommit]` — used by
  Show Custom Dialog for its button configuration.
- `; Inputs: [Table::Field "Label" password; ...]` — used by Show
  Custom Dialog for its input field specs.
- Additional bulk state surfaced in the future takes this same shape.

## Parsing precedence

Named-prefix inline tokens (Form 2) are parsed before trailing
`Kind: [...]` blocks (Form 3). A display line like

```
Go to Record/Request/Page [ Next ; Exit after last: On ; Buttons: [...] ]
```

is tokenized as:
1. Positional `Next` (Form 2 equivalent — fixed-position enum).
2. Named inline `Exit after last: On` (Form 2).
3. Named block `Buttons: [...]` (Form 3).

Form 1 (`(#id)`) is applied inside each name-bearing token — e.g. inside
the bracketed name-and-id of a named ref — and does not collide with
Form 2 or Form 3 separators.

## Zero-loss audit requirement

Every POCO author must complete this audit in the class XML doc
comment. Template:

```
/// <summary>
/// Zero-loss audit for StepName:
/// <list type="bullet">
///   <item>&lt;Step&gt; attributes (enable/id/name) — round-tripped.</item>
///   <item>&lt;Calculation&gt; CDATA — round-tripped via Calculation.</item>
///   <item>&lt;SomeElement state="..."/&gt; — Form 2 token "Some: On|Off".</item>
///   <item>&lt;Dropped/&gt; — intentionally dropped; rationale: ...</item>
/// </list>
/// </summary>
```

Items fall into exactly one of these buckets:

1. **Rendered natively by FM Pro** — FM Pro's display grammar already
   covers it; SharpFM mirrors the wording.
2. **Covered by an extension form** — one of the three above.
3. **Intentionally dropped** — rationale required. See next section for
   how to judge.

Omitting the audit is a review blocker.

## What to drop vs. surface

Hidden state is surfaced only when a user could meaningfully change it.
Some state is structurally present in XML but semantically fixed — FM
Pro never alters it, never emits it in clipboard output, and no user
workflow produces a different value. Round-tripping such state adds
visual noise for zero information.

### Canonical drop: `<Restore state="False"/>` on `If`

Upstream `agentic-fm` snippets include the element; FM Pro's own
clipboard output never does; no FM Pro user interaction produces
`state="True"`. `IfStep` drops it on both read and write. The audit
entry documents the drop:

> &lt;Restore state="False"/&gt; — intentionally dropped. FM Pro never
> changes the value and never emits the element in clipboard output; it
> carries no information worth round-tripping.

### Canonical surface: field `id` via `(#id)` suffix

`<Field table="T" id="12" name="F"/>` is a real identity — the id
selects which field is referenced, and two fields named `F` in different
tables are not interchangeable. Dropping the id would change semantics.
`FieldRef` always emits `(#12)` when an id is available.

### The heuristic

- If two valid FM Pro script states would be visually identical under
  the display grammar without the extension, **surface** the state.
- If the state has a fixed value that no user can change, **drop** it.
- If you're not sure which, **surface**. Reversing a surface → drop
  later is non-breaking; reversing a drop → surface may break tests
  users wrote against the earlier display.

## Adjacent convention: `//` disabled-step prefix

Disabled steps are prefixed with `//` in display text:

```
// Set Error Capture [ On ]
```

Parsing strips the `//` and sets `ScriptStep.Enabled = false`. This is
a document-level convention (applied to any step line) rather than a
per-step extension. Covered here for completeness.

## Implementation touch points

- `FieldRef.ToDisplayString` / `FieldRef.FromDisplayToken`
  (`src/SharpFM.Model/Scripting/Values/FieldRef.cs`) — Form 1 reference
  implementation.
- `CommentStep.ReturnGlyph`
  (`src/SharpFM.Model/Scripting/Steps/CommentStep.cs`) — the `⏎`
  (U+23CE) glyph for single-line rendering of multi-line comment text.
  An idiom adjacent to the three forms but specific to Comment.
- `ScriptLineParser.ParseLine`
  (`src/SharpFM.Model/Scripting/ScriptLineParser.cs`) — disabled-step
  prefix and bracket tokenization.
- `PerformScriptStep.FromDisplayParams`,
  `GoToLayoutStep.FromDisplayParams` — Form 1 regex parsers for named
  refs with `(#id)` suffixes.
