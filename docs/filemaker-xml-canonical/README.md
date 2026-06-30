# Canonical FileMaker XML references (vendored)

This directory vendors the authoritative, reverse-engineered description of the
native XML FileMaker Pro's Script Workspace paste handler accepts. SharpFM's
script-step model (`src/SharpFM.Model/Scripting/`) is validated against it so
that XML written back to the clipboard round-trips through FileMaker with no
silently-dropped elements.

See [`LICENSE.md`](LICENSE.md) for attribution. Both sources are CC BY 4.0,
authored by Andrew Kear / Clockwork Creative Technology.

## Layout

| Path | Source | Used for |
|------|--------|----------|
| `skill/SKILL.md`, `skill/references/*.md` | FileMaker XML Snippet Skill **v1.12** | Canonical clip-XML skeletons per step. The refresh step extracts these fenced blocks into checked-in test fixtures (under `tests/SharpFM.Tests/CanonicalSkill/fixtures/`) that the round-trip suite reads — the markdown is not parsed live at test time. **Sole source of truth for clip-XML emission.** |
| `skill/README.upstream.md` | same | The upstream project's own README, kept for provenance. |
| `inspector/FM_STEP_IDS.js` | Clockwork Inspector **v2.0** | Verbatim 217-entry `stepId → {name, category}` catalog. Used as the **working list** to align SharpFM's POCO `StepMetadata` (Id/Name/Category) and find the steps we don't yet model. Once the POCOs match it, **SharpFM's `StepMetadata` is the authoritative catalog** — this file stays only as the upstream reference for spotting drift on future FM releases. |
| `inspector/extractStepText.js` | Clockwork Inspector **v2.0** | Verbatim reference renderer for human-readable step text. Seeds SharpFM's `StepDisplayRenderer` convention. The Inspector's DDR parser is **not** reused — it targets a different XML dialect (database-design XML, not clip XML). |

## Silent-failure modes (from `skill/references/core.md`)

The paste handler is lenient as an XML parser but strict as a paste handler.
Structurally valid XML can paste with data silently dropped. The verified modes
SharpFM's renderer must respect:

- **Set Variable** — the variable name must be in a `<Name>` child, present and
  last. A bare `<n>` or misplaced name pastes with no variable name.
- **Perform JavaScript in Web Viewer** — parameters use `<Parameters Count="N"><P>…</P></Parameters>`; a `<Parameter>` element is silently dropped.
- **Install OnTimer Script** — the interval must be wrapped in `<Interval><Calculation>…</Calculation></Interval>`; a bare top-level `<Calculation>` binds to the wrong slot and the timer never fires.

## Refreshing

Run [`refresh-canonical.ps1`](refresh-canonical.ps1) to re-sync both sources
from upstream. Review the diff before committing — upstream version bumps (new
FM releases) may add steps or change canonical shapes, which is exactly the
signal the audit tests are meant to surface. After refreshing, regenerate the
test fixtures from the updated skill markdown:

```
dotnet run --file docs/filemaker-xml-canonical/extract-fixtures.cs
```

This rewrites `tests/SharpFM.Tests/CanonicalSkill/fixtures/` and reports any
catalog ids left without a fixture.
