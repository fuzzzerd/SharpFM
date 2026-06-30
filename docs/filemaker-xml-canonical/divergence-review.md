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

## Phase 6 — batch migration

### Go to Next Field (4) / Go to Previous Field (5) — id swap fixed
- **Before:** our POCOs emitted id **5** for "Go to Next Field" and **4** for
  "Go to Previous Field".
- **Now:** Next = 4, Previous = 5.
- **Sources:** both the skill and the inspector `FM_STEP_IDS` catalog agree
  (4 = Next, 5 = Previous). Confirmed before flipping.
- [ ] Reviewed

### Trigger Claris Connect Flow (211) — step id 0 → 211
- **Before:** `XmlId` was left 0 ("canonical id unconfirmed"); the step emitted
  `<Step id="0" …>`.
- **Now:** id 211 per the skill's control reference. The rest of the step's XML
  was already canonical.
- [ ] Reviewed

### Batch migration — 64 steps to the shape engine
The bulk of the remaining steps were migrated to the shape-driven renderer in
one batch. The dominant change is **no longer emitting optional/empty child
elements** (`<Field/>`, `<Calculation/>`, `<UniversalPathList/>`, dimension
calcs, etc.) that FileMaker omits from an unconfigured step's canonical form;
several steps also had element **reorders**, wrapper **nesting** (Add Account →
`<AddAccount>`, Configure AI Account → `<SetLLMAccount>`, Insert Embedding →
`<LLMEmbedding>`/`<LLMBulkEmbedding>`), a typo fix (`AccoutName` → `AccountName`
on Configure AI Account), and the field-id and Trigger-Claris-Connect-Flow id
fixes above. Every migrated step now round-trips to its skill canonical form
(the `CanonicalSkillRoundTripTests` suite is the proof); the per-step
`*Tests.cs` constants were updated to the canonical XML where they encoded the
old form.
- [ ] Reviewed (spot-check the per-step diffs in this commit)
