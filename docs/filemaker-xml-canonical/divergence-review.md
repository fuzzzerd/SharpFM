# Divergence review log

Every place SharpFM's emitted XML **changed** to match the vendored skill's
canonical form during the canonicalization audit. The policy is *trust the
skill*: where our output differed, we adopt the skill's form and record the
change here for a second review.

Each entry: the step, what we emitted **before**, what we emit **now**, the
skill section that mandates it, and a review box. Tick the box once a human has
confirmed the change is correct.

> Status legend: `[ ]` awaiting review · `[x]` reviewed & accepted · `[!]` flagged for discussion

## Phase 5 — worst offenders

### Set Variable (141) — no behavioral change
- [ ] Already canonical (skill §3). Refactored to the shape-driven renderer;
  emitted XML is byte-for-byte unchanged. Listed for completeness only.

### If (68), Else If (125), Else (69) — added `<Restore>`
- **Before:** `<Restore state="False"/>` was dropped on read and never emitted.
- **Now:** emit `<Restore state="False"/>` as the first child (before
  `<Calculation>` where present).
- **Skill:** §8.1. Round-trip tested against FM Pro 2025/2026 — FileMaker *does*
  write it. Supersedes the earlier `docs/advanced-filemaker-scripting-syntax.md`
  note that claimed FM never emits it.
- [ ] Reviewed

### Loop (71) — added `<Restore>` and `<FlushType>`
- **Before:** emitted no children.
- **Now:** emit `<Restore state="False"/>` then `<FlushType value="Always"/>`.
- **Skill:** §8.1.
- [ ] Reviewed

> Both values are fixed (FileMaker never varies them), so they are hidden from
> the display line and round-tripped in XML only.

### Install OnTimer Script (148) — element order Interval → Script
- **Before:** emitted `<Script>` then `<Interval>`.
- **Now:** emit `<Interval>` then `<Script>` (interval wrapped in `<Interval>`,
  which was already correct).
- **Skill:** §8.1 / §7.3. Out-of-order children bind the interval to the wrong
  internal slot on paste (a verified silent-failure mode).
- [ ] Reviewed

### New Window (122) — `NewWndStyles` value type + optional slots
- **Before:** emitted a text-bodied `<NewWndStyles>{raw string}</NewWndStyles>`
  and always-present empty `<Height>`/`<Width>`/`<DistanceFromTop>`/
  `<DistanceFromLeft>` elements; used generic `Calculation`…`Calculation5` fields.
- **Now:** `<NewWndStyles .../>` is the attribute-bearing element produced by the
  `NewWindowStyles` value type; `Name`, the four dimensions and `Layout` are
  optional and omitted when unset.
- **Skill:** windows reference.
- [ ] Reviewed

### Configure RAG Account (227) — inner wrapper + reorder
- **Before:** emitted `RAGAccountName`, `Endpoint`, `AccessAPIKey`,
  `VerifySSLCertificates` as flat `<Step>` children.
- **Now:** `<VerifySSLCertificates state="False"/>` first, then a
  `<ConfigureRAGAccount>` wrapper holding the (optional) account fields; empty
  wrapper when unconfigured.
- **Skill:** accounts/AI reference.
- [ ] Reviewed

### Go to Related Record (74) — dropped spurious `<Animation>`
- **Before:** appended `<Animation value="None"/>` after `<Layout>`, and always
  emitted `<Layout>` even when empty.
- **Now:** no `<Animation>` element; `<Layout>` is optional (omitted when unset);
  `<Table>` remains always-present.
- **Skill:** navigation reference (documents no Animation for this step).
- [ ] Reviewed

## Pending (flagged, not yet changed)

### Go to Next Field (4) / Go to Previous Field (5) — id swap
- Our POCOs emit id **5** for "Go to Next Field" and **4** for "Go to Previous
  Field"; the skill and the inspector catalog have them the other way round.
  Tracked by fixtures `004-GoToNextField` / `005-GoToPreviousField` in
  `KnownDivergences`. Will be corrected in the navigation batch (phase 6) and
  moved to a reviewed entry above.
- [!] Confirm the skill's ids (4 = Next, 5 = Previous) before flipping.
