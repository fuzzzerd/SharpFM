# Script Step POCO Migration — Sweep-Phase Plan

Status: completed on `fuzzz/all-pocos`. All 205 script steps are typed
`IStepFactory` POCOs discovered via `StepRegistry`. This doc is kept as
a record of the sweep's approach; new step POCOs (e.g. after a FileMaker
release that adds a step) should follow the same per-step TDD workflow
below.

## Context

The [pilot](../advanced-filemaker-scripting-syntax.md) established the
typed POCO pattern with three representative steps: `BeepStep`,
`SetErrorCaptureStep`, `IfStep`. The sweep covers the remaining 189
FileMaker script steps (206 total − 14 pre-existing POCOs − 3 pilot).
When the sweep finishes, all 206 steps are typed POCOs,
`StepCatalogLoader` and the catalog-driven helpers are deleted, and
the `step-catalog-en.json` file is no longer embedded.

The sweep is not a single PR. Each wave lands independently, leaves the
tree green, and can be reviewed in isolation.

## Tier-based ordering

Run the catalog through the tier filter from the earlier analysis (see
message history in the pilot branch's `agent/catalogs` walkthrough):

| Tier | Shape | Count | Rationale |
|---|---|---|---|
| A | Zero-param | 43 | Trivially mechanical; establish the cadence. |
| B | Only boolean/enum params | 29 | Locks in `ParamMetadata.ValidValues` patterns. |
| C | Text-bearing | 7 | Introduces text-escaping and display bracket grammar. |
| D | Calc / field / named-ref / complex | ~110 | The variety bucket. Contains real work — advanced-syntax decisions land here. |

Migrate in A → B → C → D order. Within a tier, batch by FileMaker
category (`control`, `windows`, `fields`, etc.) to keep related steps'
PRs discoverable.

## Per-step TDD workflow

For each step, in this order:

1. **Copy canonical XML** from
   `C:\source\personal\agentic-fm\agent\snippet_examples\steps\<category>\<name>.xml`
   (outside this repo — read-only reference, never committed) into an
   inline `const string` fixture in the new test class.
2. **Write the round-trip test first.** Parse the fixture → run through
   the POCO's `Metadata.FromXml!(el).ToXml()` → assert
   `XNode.DeepEquals` against the source. Fails because the POCO
   doesn't exist yet.
3. **Write the POCO** — implement `IStepFactory`, define `Metadata`,
   implement `ToXml` / `ToDisplayLine` / `FromXml` /
   `FromDisplayParams`. Pattern from `BeepStep.cs` (zero-param),
   `SetErrorCaptureStep.cs` (single-boolean), or `IfStep` (block-pair,
   calc) — pick the closest match.
4. **Add the display tests.** `ToDisplayLine()` returns expected
   string; `FromDisplay(Metadata.FromDisplay!(...))` survives a
   round-trip.
5. **Add the disabled-step test.** `enable="False"` → `step.Enabled`
   is false → `ToXml()` emits `enable="False"`.
6. **Complete the zero-loss audit** in the POCO's XML doc comment.
   Every XML element/attribute either rendered natively by FM Pro,
   covered by an advanced-syntax extension, or intentionally dropped
   with rationale (see
   [`advanced-filemaker-scripting-syntax.md`](../advanced-filemaker-scripting-syntax.md)).
7. **For Tier D steps that need advanced syntax**, land the syntax
   extension behind its own commit before the POCO that uses it, so
   reviewers can validate the grammar decision in isolation.

## Rollout cadence

- **One PR per batch of ~10 POCOs within a tier.** Smaller is too
  noisy; larger is unreviewable.
- **Tier A fits in 4-5 PRs.** Tier B in 3. Tier C in 1. Tier D is
  where PR planning gets real — expect 12-15 PRs depending on
  per-step complexity.
- **Each PR's diff is only steps + their tests.** No consumer rewires,
  no deletions (with the specific exceptions listed below).
- **Commit per POCO within a PR** so each step is individually
  revertable if the pattern needs adjusting.

## Consumer migration schedule

Consumers of the legacy `StepCatalogLoader` / `StepCatalogGenerated`
flip to `StepRegistry` as their dependencies complete:

| Consumer | Flips when | What changes |
|---|---|---|
| `FmScriptCompletionProvider` | Already migrated in pilot | — |
| `ScriptValidator` | All block-pair partners (If/End If, Loop/End Loop, Open/Commit Transaction, etc.) are POCOs | `definition.BlockPair` reads become `StepRegistry.ByName[name].BlockPair` |
| `ScriptTextParser.FromDisplayLine` | All catalog-name resolutions have POCO equivalents | Drops the `StepCatalogLoader.ByName.TryGetValue` fallback |
| `FmScript.ApplyUpdate` | Typed setters exist on every POCO | MCP apply-op dispatches to POCO-specific `With*` methods instead of generic `CatalogXmlBuilder.UpdateParam` |

Until its flip point, each consumer keeps reading the legacy surface —
the pilot's `StepRegistry` bridge populates `StepXmlFactory` /
`StepDisplayFactory` so this works transparently.

## Deletion schedule

In order:

1. **Per-wave**: as a tier completes, delete any catalog-helper code
   paths that are now exclusively serving that tier's step shapes.
   (Usually none — the helpers are shape-agnostic.)
2. **After Tier D completes** — when all 206 steps are POCOs and all
   consumers above have flipped:
   - Delete `src/SharpFM.Model/Scripting/StepCatalogLoader.cs`.
   - Delete `src/SharpFM.Model/Scripting/IStepCatalog.cs`.
   - Delete `src/SharpFM.Model/Scripting/Serialization/StepXmlFactory.cs`.
   - Delete `src/SharpFM.Model/Scripting/Serialization/StepDisplayFactory.cs`.
   - Remove the `StepRegistry → legacy factory` bridge from
     `StepRegistry.Scan()` — now dead code.
   - Delete `src/SharpFM.Model/Scripting/Steps/RawStep.cs`.
   - Delete `src/SharpFM.Model/Scripting/Steps/RawStepAllowList.cs`
     and its tests.
   - Delete `src/SharpFM.Model/Scripting/Serialization/CatalogXmlBuilder.cs`.
   - Delete `src/SharpFM.Model/Scripting/Serialization/CatalogParamExtractor.cs`.
   - Delete `src/SharpFM.Model/Scripting/Serialization/CatalogDisplayRenderer.cs`.
   - Delete `src/SharpFM.Model/Scripting/Serialization/CatalogValidator.cs`.
   - Remove the `<EmbeddedResource Include="Scripting\Catalog\step-catalog-en.json" />`
     line from `src/SharpFM.Model/SharpFM.Model.csproj`.
   - Delete `src/SharpFM.Model/Scripting/Catalog/step-catalog-en.json`.
   - Introduce `UnknownStep` in place of `RawStep` for forward-compat
     against future FileMaker step additions (wraps raw XElement;
     same semantics as the retired `RawStep` when `Definition == null`).
3. **After ship verification** — `StepDefinition` / `StepParam` /
   `StepBlockPair` / `BlockPairRole` / `StepEnumValue` no longer have
   any catalog-side producers. They may be kept (used by
   `StepMetadata.BlockPair`) or retired depending on whether the new
   metadata types fully absorb them. Expect a small cleanup commit
   here.

## Coverage verification

One test asserts every step catalogued by upstream is present in our
registry:

```csharp
[Fact]
public void StepRegistry_CoversFullCatalog()
{
    var expected = File.ReadAllText("path/to/canonical/step-list.json");
    var names = JsonSerializer.Deserialize<string[]>(expected);
    foreach (var name in names)
        Assert.True(StepRegistry.ByName.ContainsKey(name),
            $"Missing POCO for step '{name}'");
}
```

The canonical list is a snapshot of agentic-fm step names taken at
sweep kickoff (checked in as a one-off fixture — not the same as the
JSON catalog, just names + ids). As steps are added to upstream later,
the fixture is updated deliberately, preventing silent drift.

## Regression containment

The pilot's `FmScriptCompletionProvider` regression (only 3 steps
suggested in completions) resolves as the sweep progresses. Gate each
sweep PR on **monotonically increasing completion coverage**:

- `CompletionProviderTests` has an `EmptyLine_SuggestsPocoStepNames`
  test that enumerates all expected pilot step names.
- Each sweep PR adds the new step names to that test's expected list.
- A PR that removes a name from the list is a review blocker unless
  justified (e.g. a step was consolidated).

After Tier A merges, the list is 43 + 14 + 3 = 60 steps. After Tier B,
89. After Tier C, 96. After Tier D, 206. Upstream adds thereafter append.

## Risks and gotchas (filled in during pilot execution)

> This section gets populated with concrete discoveries from pilot
> implementation. Left as a placeholder until pilot lands.

Observed so far:

- **`StepRegistry` lazy init doesn't run before pre-existing consumers
  touch `StepXmlFactory`.** Mitigation: single `[ModuleInitializer]`
  on `StepRegistry` itself. Acceptable — one initializer on the
  registry, not per-POCO.
- **Legacy `FmScript.ToDisplayLines` reads `step.Definition.BlockPair`
  for indent decisions.** POCOs passing `Definition = null` break
  indentation on block-pair steps. Mitigation for pilot: project a
  minimal `StepDefinition` with just `BlockPair` populated into
  `base(...)`. Cleanup: when `FmScript.ToDisplayLines` migrates to
  `StepRegistry`, drop the synthesized `Definition`.
- **Tests that used `Beep` as a "catalog-but-not-POCO" canary break
  when Beep migrates.** Mitigation: swap to `Halt Script` (or
  whatever zero-param step is still on the catalog-only path). Expect
  similar swaps as each subsequent zero-param step migrates.

## Open questions deferred from pilot

- **Plugin-scenario multi-assembly registration.** Today
  `StepRegistry.Scan()` reflects only the model assembly. If a plugin
  ships its own POCOs (future scenario), should the registry scan
  loaded plugin assemblies too? Probable answer: yes, with an
  `RegisterAssembly(Assembly)` public method plugins call on load.
  Address when the first plugin author asks for it.
- **`Metadata.Notes` wiring into UI.** The pilot populates `StepNotes`
  on `SetErrorCaptureStep` and `IfStep`. No UI reads it yet. Tooltip
  and hover integration is a post-sweep concern — sequencing depends
  on Monaco / Avalonia tooltip API decisions outside this migration's
  scope.
- **`Metadata.MonacoSnippet` or synthesis.** Pilot dropped the
  catalog's `MonacoSnippet` field entirely; completion falls back to
  the step name for self-closing steps and provides nothing for
  multi-param ones. Options: (a) let each POCO author an optional
  snippet string, (b) synthesize at runtime from `ParamMetadata`, (c)
  defer snippets until someone complains. Recommend (c) — the three
  pilot POCOs work fine without; revisit after Tier B lands.
- **When to retire `StepDefinition`/`StepParam` records.** The new
  metadata types (`StepMetadata` / `ParamMetadata`) are intended to
  replace them, but `StepMetadata.BlockPair` currently reuses
  `StepBlockPair` and `IfStep` still synthesizes a `StepDefinition`
  for legacy `FmScript.ToDisplayLines`. Clean-up sequence TBD —
  probably a small follow-up commit after the legacy factories are
  deleted.
