# Design: Object Model as Source of Truth

## Problem

SharpFM currently treats **display text as canonical** during script editing. The roundtrip is:

```
XML → display text → [user edits text] → re-parse to model → XML
```

This works for the text editor but makes programmatic access (agents via MCP, plugins, structured editors) fragile — they'd need to manipulate bracket syntax and parse display text. The object model (`FmScript`, `ScriptStep`, `StepParamValue`) already exists but is derived, not authoritative.

## Goal

Make the **object model the single source of truth** for all clip content. All editors, agents, and plugins read from and write to the model. Text and XML become projections (renderings) of the model, not the other way around.

```
                    ┌─── Script Text Editor (render/parse)
                    │
Object Model ───────┼─── XML Editor (render/parse)
   (truth)          │
                    ├─── Table/Field DataGrid (render/bind)
                    │
                    ├─── Agent / MCP (structured API)
                    │
                    └─── Plugin API (IPluginHost)
```

## Scope

This design covers:
- What the object model owns and how it's mutated
- The contract between the model and its editors/consumers
- How this integrates with the existing plugin system
- How MCP/agent access would work against this model

This design does NOT cover:
- Specific MCP tool definitions
- UI changes to switch between editors
- Detailed implementation of each editor adapter

---

## Core Concepts

### The Clip Model

A clip is already represented by `FileMakerClip` (name, format, XML) wrapped in `ClipViewModel` (editor state, change detection). The model-as-truth change deepens this: instead of `ClipViewModel` holding a text document that IS the state, it holds a **typed model** that renderers project into text/grid/XML.

For script clips, the typed model is `FmScript` (a list of `ScriptStep` objects).
For table clips, the typed model is `FmTable` (a list of `FmField` objects).
For other clip types, the model is the raw XML string (unchanged from today).

### Mutation Paths

Every change to clip content goes through the model:

| Source | How it mutates | Example |
|--------|---------------|---------|
| Script text editor | Parse changed text → update `FmScript.Steps` | User types a new line |
| XML editor | Parse XML → rebuild model | User edits raw XML |
| Table DataGrid | Direct property mutation on `FmField` | User changes field type |
| Agent (MCP) | Structured API call → model mutation | "Add If step at index 3" |
| Plugin | `IPluginHost.UpdateSelectedClipXml()` or future model API | Plugin reformats XML |

### Rendering (Model → View)

After any mutation, all **other** views re-render from the model:

| View | Renders from |
|------|-------------|
| Script text editor | `FmScript.ToDisplayText()` |
| XML editor | `FmScript.ToXml()` (or raw XML for non-script clips) |
| Table DataGrid | Binding to `FmTable.Fields` collection |
| Plugin panels | `ClipContentChanged` event with `ClipInfo` snapshot |

The view that **caused** the mutation does NOT re-render from the model. It already has the user's state. This is the "origin tagging" pattern already used in the plugin system.

---

## Editor Contract

### Saved/Unsaved State

Each editor maintains a **local buffer** that is either **clean** (matches the model) or **dirty** (user has unsaved changes). The model only changes when the user explicitly **saves** or when an external mutation occurs.

- **Clean**: editor content matches the model. No risk of data loss.
- **Dirty**: editor has unsaved changes that haven't been flushed to the model.

This is NOT a live/real-time sync. Edits are batched — the user works freely in their editor and saves when ready. The model updates discretely, not on every keystroke.

### Save (User Action)

When the user saves (Ctrl+S, button, or editor-specific trigger):

1. Editor **parses** its local state into the model (`FmScript.FromDisplayText()` or `FmScript.FromXml()`).
2. Model **replaces** its state with the parsed result.
3. Model fires `StepsChanged` event.
4. Other views **re-render** from the updated model.
5. The saving editor is marked **clean**.

### External Mutation (Agent, Plugin, Other Editor)

When the model changes from any external source:

1. Model fires `StepsChanged` event with origin tag.
2. All editors **re-render** from the model, replacing their local buffer.
3. **Unsaved changes in the active editor are lost.** This is acceptable — external mutations are discrete operations (agent commands, plugin actions), not continuous collaborative edits. The user can see the change and continue editing.
4. **Cursor position is preserved.** After re-rendering, restore the cursor offset (clamped to the new text length). This avoids disorienting the user.
5. All editors are marked **clean** after re-render.

### Dirty State and External Mutations

If an editor is dirty and an external mutation arrives, the editor re-renders and the unsaved changes are lost. This is the explicit contract: **save early if you want to keep your work.** The UI should indicate dirty state (e.g., dot on tab, modified indicator) so the user knows they have unsaved changes.

Future consideration: we could prompt "You have unsaved changes — discard?" before applying external mutations. But for the initial implementation, the simpler model (external wins) is sufficient.

### Editor Transitions

When the user switches from editor A to editor B:

1. If editor A is **dirty**, prompt to save or discard (or auto-save — TBD).
2. Editor B **renders** from the model.
3. Editor B becomes the active editor, marked **clean**.

### No Debounce Needed

Since the model only changes on explicit save or external mutation, there's no need for debounced sync timers. The generation counter / debounce pattern currently in `ClipViewModel` can be removed in favor of this simpler contract.

---

## Model Mutation API

The `FmScript` model needs a mutation API beyond the current factory methods. These operations are what editors, agents, and plugins would use:

### Script Operations

```
AddStep(int index, ScriptStep step)
RemoveStep(int index)
MoveStep(int fromIndex, int toIndex)
UpdateStep(int index, ScriptStep replacement)
ReplaceSteps(IReadOnlyList<ScriptStep> steps)  // bulk: parse from text/XML
```

### Step-Level Operations

```
SetEnabled(bool enabled)
SetParamValue(string paramName, string? value)
```

### Query Operations (for agents/plugins)

```
FindSteps(string stepName) → IReadOnlyList<(int index, ScriptStep step)>
GetStep(int index) → ScriptStep
StepCount → int
```

### Change Notification

```
event StepsChanged  // fired after any mutation, with change details
```

The `StepsChanged` event carries enough info for renderers to decide whether to do a full re-render or a targeted update (e.g., single step changed at index 5).

---

## Table/Field Model

Tables already have a typed model (`FmTable` with `FmField` list). The same principles apply:

- `FmTable` is the source of truth
- DataGrid binds to `FmTable.Fields`
- XML editor renders from `FmTable.ToXml()`
- Agent/MCP can call `AddField()`, `RemoveField()`, `UpdateField()`

The table model is already closer to this architecture than scripts are.

---

## Integration with Plugin System

### Current: XML-level

Plugins interact via `IPluginHost.UpdateSelectedClipXml(xml, originId)`. This stays as-is for backwards compatibility and for plugins that want to work with raw XML.

### Future: Model-level (additive)

Add model-aware methods to `IPluginHost`:

```
IFmScript? GetScriptModel()          // null if not a script clip
IFmTable? GetTableModel()            // null if not a table clip
void UpdateScriptModel(Action<IFmScript> mutation, string originId)
void UpdateTableModel(Action<IFmTable> mutation, string originId)
```

The `Action<IFmScript>` pattern lets plugins make multiple mutations atomically — the model fires a single change event after the action completes.

Plugins compiled against the old API still work (XML-level). New plugins can use the model API for structured access. The host translates between them: an XML push re-parses the model; a model mutation re-derives the XML for legacy listeners.

### MCP Tools

MCP tools would wrap the same model API:

```
sharpfm_script_add_step(index, step_name, params)
sharpfm_script_remove_step(index)
sharpfm_script_find_steps(step_name) → list of steps
sharpfm_script_get_all_steps() → full script model
sharpfm_table_add_field(name, type, ...)
sharpfm_table_get_fields() → field list
```

These are thin wrappers over the mutation/query API on the model.

---

## Migration Path

### Phase A — Model as hub for scripts

1. `FmScript` gains mutation methods (`AddStep`, `RemoveStep`, etc.)
2. `FmScript` gains `StepsChanged` event
3. `ScriptClipEditor` refactored: text changes → parse → `ReplaceSteps()` on model; model changes → re-render text (unless self-originated)
4. XML editor for scripts: XML changes → `FmScript.FromXml()` → `ReplaceSteps()`; model changes → `FmScript.ToXml()` → re-render XML
5. All existing tests still pass — roundtrip behavior unchanged

### Phase B — Model-level plugin/agent API

6. Add `GetScriptModel()` / `UpdateScriptModel()` to `IPluginHost`
7. Add MCP tool definitions that wrap the model API
8. Origin tagging extended to model-level mutations

### Phase C — Table model alignment

9. Apply the same pattern to `FmTable` (already mostly there)
10. `TableClipEditor` refactored to same hub pattern
11. Add `GetTableModel()` / `UpdateTableModel()` to `IPluginHost`

### Phase D — Typed step accessors

12. Add convenience accessors on `ScriptStep` for common patterns:
    - `GetCalculation()`, `GetFieldReference()`, `GetScriptReference()`
    - `GetNamedParam(string label)` — typed wrapper over `ParamValues`
13. These are additive — no breaking changes, just ergonomic API for agents/plugins

---

## What Doesn't Change

- `FileMakerClip` still holds the XML string as the persistence format
- `ClipRepository` / `IClipRepository` still loads/saves XML
- `StepCatalog` / `StepDefinition` / `StepParam` unchanged
- Handler registry unchanged (handlers render/serialize, they don't own state)
- Plugin `IPluginHost.UpdateSelectedClipXml()` still works
- Display text format (`Step [ param ; param ]`) unchanged
- All existing tests pass at each phase
