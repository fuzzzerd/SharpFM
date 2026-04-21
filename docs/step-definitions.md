# FileMaker Script Step — Typed POCO Migration Tracker

This document is the working checklist for migrating FileMaker script steps from the generic `RawStep` fallback to typed POCOs under `src/SharpFM.Model/Scripting/Steps/`. It exists to drive TDD: each step block holds a placeholder for verbatim FileMaker Pro clipboard XML, a list of the expected display-text form, and a checklist for the small refactor needed to land the typed POCO.

**Rule of thumb:** we only add a typed POCO when a step has behavior the generic catalog-driven pipeline can't express (named references with ids, discriminated unions, bespoke display formats, nested calculations, custom parsing). Simple steps stay on `RawStep` indefinitely — that's fine, `RawStep` is fully lossless for the source XML.

---

## How to use this document

For each step you want to migrate:

1. **Copy verbatim XML from FileMaker Pro.** Create the step in the script workspace, right-click the step in the script and choose Copy, then paste directly into the sample block here. Do this once per meaningful permutation (e.g. each `LayoutDestination` for Go to Layout, each `RowPageLocation` for Go to Record).
2. **Write failing tests first.** Create `tests/SharpFM.Tests/Scripting/Steps/{StepName}StepTests.cs` with the verbatim fixture as a constant and one test per behavior assertion: display rendering, round-trip XML preservation, and display-text → XML parsing.
3. **Run the tests and watch them fail.** They'll fail against the current `RawStep` path with whatever generic output the catalog produces.
4. **Add the typed POCO.** Create `src/SharpFM.Model/Scripting/Steps/{StepName}Step.cs` following the `GoToLayoutStep` pattern. Register with `StepXmlFactory` and `StepDisplayFactory` via a `[ModuleInitializer]`.
5. **Run the tests and watch them pass.**
6. **Check the regression list.** Any previously-failing test assertions for this step (see the tests under `tests/SharpFM.Tests/Scripting/` that failed after the `StepParamValue` deletion) should come back green.
7. **Check the box** on this document.

## Reference: value types already defined

Reuse these wherever they fit — don't create new ones unless the step genuinely needs a new shape.

| Type | Location | Purpose |
|---|---|---|
| `NamedRef(int Id, string Name)` | `Values/NamedRef.cs` | id+name pairs for Script, Layout (named), TableOccurrence refs |
| `FieldRef(Table?, Id, Name, VariableName?)` | `Values/FieldRef.cs` | field or variable, with `ToDisplayString()` → `Table::Field` / `$var` |
| `Calculation(string Text)` | `Values/Calculation.cs` | CDATA-wrapped calc expression |
| `Animation(string WireValue)` | `Values/Animation.cs` | raw wire-string for FM animation names (unknown values pass through) |
| `LayoutTarget` (sealed record hierarchy) | `Values/LayoutTarget.cs` | Original / Named / ByNameCalc / ByNumberCalc |

## Reference: architecture pattern

See `src/SharpFM.Model/Scripting/Steps/GoToLayoutStep.cs` as the canonical example. Every typed POCO has:

- Typed properties (no `XElement`, no string bags)
- `public static new ScriptStep FromXml(XElement step)` — parse from source XML
- `public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)` — parse from display-text tokens
- `public override XElement ToXml()` — pure function of the typed state
- `public override string ToDisplayLine()` — pure function of the typed state
- A `[ModuleInitializer]` method that calls `StepXmlFactory.Register` and `StepDisplayFactory.Register`

## Reference: display extension style guide

See [`advanced-filemaker-scripting-syntax.md`](advanced-filemaker-scripting-syntax.md) for the full spec — the three extension forms (`(#id)` suffix, inline word tokens, trailing `; Kind: [...]` blocks), parsing precedence, the zero-loss audit every POCO must complete, and the judgment call on when to drop hidden state rather than surface it.

---

## Priority 1 — handler-backed steps (failing regressions)

These 13 steps used to have bespoke display rendering or display-text parsing via the retired `Handlers/` tree. Their tests are currently failing because the generic catalog path can't reproduce their special formatting. Each one needs a typed POCO.

P1 regression status:

| Previously failing test | Status | Fixed by |
|---|---|---|
| `ScriptStepTests.SetField_FromXml_ToDisplayLine` | ✓ | `SetFieldStep` |
| `ScriptStepTests.SetVariable_FromXml_ToDisplayLine` | ✓ | `SetVariableStep` |
| `ScriptStepTests.SetVariable_WithRepetition_ToDisplayLine` | ✓ | `SetVariableStep` |
| `ScriptStepTests.PerformScript_FromXml_ToDisplayLine` | ✓ | `PerformScriptStep` |
| `FmScriptModelTests.FromDisplayText_ToXml_SetVariable` | ✓ | `SetVariableStep` |
| `FmScriptModelTests.FromXml_ToDisplayText_IfEndIf_Indented` | ✓ | `ControlFlowSteps` |
| `FmScriptModelTests.RoundTrip_RealisticScript` | ✓ | All above + Loop / End Loop typed POCOs |

---

### Set Field *(#76)*

- [x] **Typed POCO landed** — `SetFieldStep.cs`, tests in `SetFieldStepTests.cs`

**Why it's special:** FM Pro's display format is `Set Field [ Target ; Calculation ]` — the Field param appears *before* the Calculation param, even though the catalog order is [Calculation, Field]. The old `SetFieldHandler.ToDisplayLine` reordered these explicitly.

**Fields to carry:** `FieldRef Target`, `Calculation Expression`. Both required (though tolerant of missing for partial input).

**Verbatim XML samples** *(captured from FM Pro via Raw Clipboard Viewer)*:

```xml
<!-- Simple literal: Set Field [ ScriptDefinitionHelper::ModifiedBy ; "just-a-string" ] -->
<Step enable="True" id="76" name="Set Field">
  <Calculation><![CDATA["just-a-string"]]></Calculation>
  <Field table="ScriptDefinitionHelper" id="5" name="ModifiedBy"></Field>
</Step>
```

```xml
<!-- Calc with variable: Set Field [ ScriptDefinitionHelper::ModifiedBy ; $variable + " string" ] -->
<Step enable="True" id="76" name="Set Field">
  <Calculation><![CDATA[$variable + " string"]]></Calculation>
  <Field table="ScriptDefinitionHelper" id="5" name="ModifiedBy"></Field>
</Step>
```

```xml
<!-- Multi-line calc concatenating fields: Set Field [ ScriptDefinitionHelper::ModifiedBy ; ScriptDefinitionHelper::PrimaryKey & " " & ScriptDefinitionHelper::CreatedBy ] -->
<Step enable="True" id="76" name="Set Field">
  <Calculation><![CDATA[ScriptDefinitionHelper::PrimaryKey 
& " " 
& ScriptDefinitionHelper::CreatedBy]]></Calculation>
  <Field table="ScriptDefinitionHelper" id="5" name="ModifiedBy"></Field>
</Step>
```

**Expected display forms:**

- `Set Field [ ScriptDefinitionHelper::ModifiedBy ; "just-a-string" ]`
- `Set Field [ ScriptDefinitionHelper::ModifiedBy ; $variable + " string" ]`
- `Set Field [ ScriptDefinitionHelper::ModifiedBy ; ScriptDefinitionHelper::PrimaryKey & " " & ScriptDefinitionHelper::CreatedBy ]` (multi-line calc preserves internal newlines in CDATA but renders on one line for the display form)
- `Set Field [ MyField ]` (no calc — degenerate case, still worth testing)

**Notes on the captured XML:**

- The `Field` element carries `table`, `id`, and `name` attributes — all three must round-trip (id is the lossless anchor, matching the `(#id)` convention used by `GoToLayoutStep`).
- `Calculation` content is wrapped in CDATA and may contain literal newlines (sample 3). The typed POCO's `Calculation.Text` should preserve those newlines byte-for-byte.
- Param order in the source XML is `[Calculation, Field]` but FM Pro's display is `[ Field ; Calculation ]` — this reorder is exactly what `SetFieldHandler.ToDisplayLine` used to do.

---

### Set Variable *(#141)*

- [x] **Typed POCO landed** — `SetVariableStep.cs`, tests in `SetVariableStepTests.cs`

**Why it's special:** The canonical display is `Set Variable [ $name[rep] ; Value: calc ]` — the `[rep]` suffix is shown only when repetition is set and not equal to 1. The catalog params are `Value` (namedCalc), `Repetition` (namedCalc), `Name` (text), which the old `SetVariableHandler` reordered and rewrapped into the display form above. Parsing back is also bespoke (`ParseVarRepetition` splits `$name[rep]`).

**Fields to carry:** `string Name`, `Calculation Value`, `Calculation Repetition` (default `"1"`).

**Verbatim XML samples** *(captured from FM Pro via Raw Clipboard Viewer)*:

```xml
<!-- Simple (repetition = 1): Set Variable [ $count ; Value: 0 ] -->
<Step enable="True" id="141" name="Set Variable">
  <Value>
    <Calculation><![CDATA[0]]></Calculation>
  </Value>
  <Repetition>
    <Calculation><![CDATA[1]]></Calculation>
  </Repetition>
  <Name>$count</Name>
</Step>
```

```xml
<!-- With literal repetition: Set Variable [ $arr[3] ; Value: "third" ] -->
<Step enable="True" id="141" name="Set Variable">
  <Value>
    <Calculation><![CDATA["third"]]></Calculation>
  </Value>
  <Repetition>
    <Calculation><![CDATA[3]]></Calculation>
  </Repetition>
  <Name>$arr</Name>
</Step>
```

```xml
<!-- Repetition is itself a calculation: Set Variable [ $arr[$anotherVariable] ; Value: $count + 5 ] -->
<Step enable="True" id="141" name="Set Variable">
  <Value>
    <Calculation><![CDATA[$count + 5]]></Calculation>
  </Value>
  <Repetition>
    <Calculation><![CDATA[$anotherVariable]]></Calculation>
  </Repetition>
  <Name>$arr</Name>
</Step>
```

**Expected display forms:**

- `Set Variable [ $count ; Value: 0 ]`
- `Set Variable [ $arr[3] ; Value: "third" ]`
- `Set Variable [ $arr[$anotherVariable] ; Value: $count + 5 ]`
- `Set Variable [ $x ]` (no value — degenerate)

**Notes on the captured XML:**

- XML param order is `[Value, Repetition, Name]`; display reorders to `[ Name[rep] ; Value: calc ]`.
- `Repetition` is a full `Calculation`, not an integer — it can be a literal (`1`, `3`) or any calc expression (`$anotherVariable`, `$count + 5`). The typed POCO should carry `Calculation Repetition`, not `int`.
- `Repetition` is *always present* in the source XML, even when it equals `1`. The display-line writer must suppress `[rep]` when `Repetition.Text == "1"` (literal-one check, not arithmetic), and the XML writer must always emit a `<Repetition>` element (round-trip requirement).
- `Name` carries the `$` prefix as part of the text content — do not strip it on parse; treat the whole string as the variable name. Parsing `$arr[3]` or `$arr[$anotherVariable]` back from display text means splitting on the outermost `[` / `]` to separate name from repetition calc (watch for nested brackets in the repetition expression).
- Both `Value` and `Repetition` wrap their `Calculation` in a named wrapper element (`<Value><Calculation/></Value>`) — this is the `namedCalc` catalog shape.

---

### Perform Script *(#1)*

- [x] **Typed POCO landed** — `PerformScriptStep.cs`, tests in `PerformScriptStepTests.cs`

**Why it's special:** Two discriminated modes (like Go to Layout's `LayoutTarget`):

1. **Static reference** — a named `<Script id=".." name=".."/>` element picks the script, optional parameter calc rides as a bare `<Calculation>` sibling.
2. **By calculation** — no `<Script>` element; a `<Calculated><Calculation>…</Calculation></Calculated>` wrapper carries the *name-resolving* calculation, plus the same optional parameter `<Calculation>` sibling.

Display format `Perform Script [ "ScriptName" ; Parameter: <calc> ]` for static; the calculated form renders with `By Name:` semantics (exact FM Pro rendering to confirm — we don't have a captured display-text sample yet).

**Fields to carry:** `PerformScriptTarget Target` (discriminated union: `ByReference(NamedRef)` / `ByCalculation(Calculation)`), `Calculation? Parameter`.

**Verbatim XML samples** *(captured from FM Pro via Raw Clipboard Viewer)*:

```xml
<!-- Static "From list" reference with parameter:
     Perform Script [ Specified: From list ; "Dummy-Script-For-Reference" ; Parameter: $$SomeGlobalVariable ] -->
<Step enable="True" id="1" name="Perform Script">
  <Calculation><![CDATA[$$SomeGlobalVariable]]></Calculation>
  <Script id="4" name="Dummy-Script-For-Reference"></Script>
</Step>
```

```xml
<!-- "By name" (calculated) reference with parameter:
     Perform Script [ Specified: By name ; $$globalVar & " literal-string" ; Parameter: $$SomeGlobalVariable ] -->
<Step enable="True" id="1" name="Perform Script">
  <Calculated>
    <Calculation><![CDATA[$$globalVar & " literal-string"]]></Calculation>
  </Calculated>
  <Calculation><![CDATA[$$SomeGlobalVariable]]></Calculation>
</Step>
```

```xml
<!-- Static "From list" reference without parameter:
     Perform Script [ Specified: From list ; "Dummy-Script-For-Reference" ; Parameter: ] -->
<Step enable="True" id="1" name="Perform Script">
  <Script id="4" name="Dummy-Script-For-Reference"></Script>
</Step>
```

```xml
<!-- "By name" reference without parameter:
     Perform Script [ Specified: By name ; $$globalVar & " literal-string" ; Parameter: ] -->
<Step enable="True" id="1" name="Perform Script">
  <Calculated>
    <Calculation><![CDATA[$$globalVar & " literal-string"]]></Calculation>
  </Calculated>
</Step>
```

**Expected display forms** *(FM Pro's native wording confirmed; SharpFM appends `(#id)` on static refs per the style guide)*:

- `Perform Script [ Specified: From list ; "Dummy-Script-For-Reference" (#4) ; Parameter: $$SomeGlobalVariable ]`
- `Perform Script [ Specified: By name ; $$globalVar & " literal-string" ; Parameter: $$SomeGlobalVariable ]` *(no id — by-name form has no `<Script>` element)*
- `Perform Script [ Specified: From list ; "Dummy-Script-For-Reference" (#4) ; Parameter: ]` *(note trailing empty `Parameter:` label)*
- `Perform Script [ Specified: By name ; $$globalVar & " literal-string" ; Parameter: ]`

**Notes on the captured XML:**

- The parameter is a *bare, unwrapped* `<Calculation>` element — it does **not** live inside a `<Parameter>` wrapper. This differs from Set Variable's `<Value><Calculation/></Value>` namedCalc shape.
- **Parameter omission is by absence of the element**, not by empty CDATA. When the user leaves Parameter blank, the sibling `<Calculation>` element is entirely missing from the XML. On write: emit `<Calculation>` only when `Parameter` is non-null.
- The `Parameter:` label is **always present in the display line**, even when the parameter is omitted. Trailing form is literally `; Parameter: ]` with a space after the colon. Display writer must always emit the label; parser must accept empty.
- `Specified:` discriminant is literal in the display line — `Specified: From list` for static, `Specified: By name` for calculated. This is the display-level signal matching the XML-level `<Script>` vs. `<Calculated>` discriminant.
- The `<Script>` element carries `id` + `name`. FM Pro's own display line is name-only (`"Dummy-Script-For-Reference"`). Under the zero-loss rule and the style guide (form 1: id annotation on a named ref), SharpFM renders it as `"Dummy-Script-For-Reference" (#4)` — same `(#id)` convention as `GoToLayoutStep`. Expected display forms above should be updated to include the `(#id)` suffix when the POCO is written.
- The `<Calculated>` wrapper is the discriminant for by-calculation mode. Presence of `<Script>` vs. `<Calculated>` selects the union variant; they are mutually exclusive in FM Pro output.
- Element order: FM Pro emits parameter `<Calculation>` *before* `<Script>` in the static form (sample 1), but `<Calculated>` *before* parameter `<Calculation>` in the calculated form (sample 2). Parser should be order-tolerant; writer should match FM Pro's order per-variant for clipboard-roundtrip fidelity.

---

### Go to Record/Request/Page *(#16)*

- [x] **Typed POCO landed** — `GoToRecordStep.cs`, tests in `GoToRecordStepTests.cs`

**Why it's special:** Display is shaped by a combination of `RowPageLocation` value *and* which optional child elements are present — not by boolean values alone. The old handler treated `Exit`/`NoInteract` as booleans with a default-suppression rule, but the captured fixtures prove it's actually an **element-presence** rule that varies per location:

- `First` / `Last` — only the location name is rendered.
- `Next` / `Previous` — `<Exit>` is always emitted by FM Pro, so `Exit after last: On|Off` is always rendered.
- `ByCalculation` — `With dialog: On|Off` is always rendered (inverted from `<NoInteract state>`: `NoInteract=False` → `With dialog:On`; `NoInteract=True` → `With dialog:Off`), followed by the `<Calculation>` text.

**Fields to carry:** `RowPageLocationKind Location` (enum: First / Last / Previous / Next / ByCalculation), `Calculation? LocationCalc` (only when `Location == ByCalculation`), `bool? ExitAfterLast` (nullable: `null` = `<Exit>` absent; value = state). `NoInteract` is always present in the XML so it can be a plain `bool NoInteract` with rendering logic keyed off the location.

**Verbatim XML samples** *(captured from FM Pro via Raw Clipboard Viewer)*:

```xml
<!-- Go to Record/Request/Page [First] -->
<Step enable="True" id="16" name="Go to Record/Request/Page">
  <NoInteract state="False"></NoInteract>
  <RowPageLocation value="First"></RowPageLocation>
</Step>
```

```xml
<!-- Go to Record/Request/Page [Next; Exit after last:On] -->
<Step enable="True" id="16" name="Go to Record/Request/Page">
  <NoInteract state="False"></NoInteract>
  <Exit state="True"></Exit>
  <RowPageLocation value="Next"></RowPageLocation>
</Step>
```

```xml
<!-- Go to Record/Request/Page [Next; Exit after last:Off] -->
<Step enable="True" id="16" name="Go to Record/Request/Page">
  <NoInteract state="False"></NoInteract>
  <Exit state="False"></Exit>
  <RowPageLocation value="Next"></RowPageLocation>
</Step>
```

```xml
<!-- Go to Record/Request/Page [Previous; Exit after last:On] -->
<Step enable="True" id="16" name="Go to Record/Request/Page">
  <NoInteract state="False"></NoInteract>
  <Exit state="True"></Exit>
  <RowPageLocation value="Previous"></RowPageLocation>
</Step>
```

```xml
<!-- Go to Record/Request/Page [Previous; Exit after last:Off] -->
<Step enable="True" id="16" name="Go to Record/Request/Page">
  <NoInteract state="False"></NoInteract>
  <Exit state="False"></Exit>
  <RowPageLocation value="Previous"></RowPageLocation>
</Step>
```

```xml
<!-- Go to Record/Request/Page [Last] -->
<Step enable="True" id="16" name="Go to Record/Request/Page">
  <NoInteract state="False"></NoInteract>
  <RowPageLocation value="Last"></RowPageLocation>
</Step>
```

```xml
<!-- Go to Record/Request/Page [With dialog:On; $someVar + 3] -->
<Step enable="True" id="16" name="Go to Record/Request/Page">
  <NoInteract state="False"></NoInteract>
  <RowPageLocation value="ByCalculation"></RowPageLocation>
  <Calculation><![CDATA[$someVar + 3]]></Calculation>
</Step>
```

```xml
<!-- Go to Record/Request/Page [With dialog:Off; $someVar + 3] -->
<Step enable="True" id="16" name="Go to Record/Request/Page">
  <NoInteract state="True"></NoInteract>
  <RowPageLocation value="ByCalculation"></RowPageLocation>
  <Calculation><![CDATA[$someVar + 3]]></Calculation>
</Step>
```

**Expected display forms** *(confirmed against FM Pro)*:

- `Go to Record/Request/Page [First]` *(bare — no `<Exit>`, no suffix)*
- `Go to Record/Request/Page [Last]` *(bare — no `<Exit>`, no suffix)*
- `Go to Record/Request/Page [Next; Exit after last:On]`
- `Go to Record/Request/Page [Next; Exit after last:Off]`
- `Go to Record/Request/Page [Previous; Exit after last:On]`
- `Go to Record/Request/Page [Previous; Exit after last:Off]`
- `Go to Record/Request/Page [With dialog:On; $someVar + 3]`
- `Go to Record/Request/Page [With dialog:Off; $someVar + 3]`

**Notes on the captured XML:**

- `<NoInteract state="True|False">` is **always present** in the XML regardless of location. It only affects the display when `Location == ByCalculation` (rendered as `With dialog:On|Off`, inverted from its state). For non-calc locations, it's carried through the round-trip but never shown.
- **Zero-loss proof for `NoInteract` on non-calc locations:** FM Pro emits `<NoInteract state="False">` on every non-`ByCalculation` step and never surfaces a UI control to toggle it in that configuration. A POCO parsed from display text for a non-calc location therefore defaults to `NoInteract=False` with no reachable path to produce `True`. No extension form needed; the defaulting is proven equivalent to FM Pro's behavior.
- `<Exit state="True|False">` is emitted by FM Pro **only for Next and Previous** locations (absent on First / Last / ByCalculation). For those two locations it is always present, so the POCO can carry a plain `bool ExitAfterLast` and the XML writer conditions the element's emission on `Location ∈ { Next, Previous }`. The parser should tolerate `<Exit>` if it ever appears alongside other locations, but the writer should match FM Pro's emission rule.
- `<RowPageLocation value="...">` values are fixed strings: `First`, `Last`, `Previous`, `Next`, `ByCalculation`. Round-trip these verbatim; don't translate to internal enum names on write.
- `<Calculation>` (CDATA) is present *only* when `RowPageLocation=ByCalculation` and carries the target row/request/page expression.
- Element order in FM Pro output: `NoInteract`, then optional `Exit`, then `RowPageLocation`, then optional `Calculation`. Parser should be order-tolerant; writer should match for clipboard fidelity.

---

### Show Custom Dialog *(#87)*

- [x] **Typed POCO landed** — `ShowCustomDialogStep.cs`, tests in `ShowCustomDialogStepTests.cs`

**Why it's special:** The display text is a thin projection over a much richer XML state. Many "Specify" dialog knobs in FM Pro have no effect on the rendered display line but must still round-trip through the XML. Structurally, this step has:

- Two `namedCalc` params (`<Title>`, `<Message>`) wrapping `<Calculation>` CDATA.
- A `<Buttons>` container that is **always exactly 3 slots** — unused buttons are emitted as `<Button CommitState="False"></Button>` (element present, inner `<Calculation>` absent).
- An **optional** `<InputFields>` container. When present, it is also **always exactly 3 slots**; unused slots are `<InputField UsePasswordCharacter="False"><Field table="" id="0" name=""></Field></InputField>`. When no input fields are configured at all, the entire `<InputFields>` element is omitted.
- `<InputField>` has a discriminated `<Field>` child: either **variable-style** (text content is `$var` / `$$var`, no attributes, optional `repetition="N"` attr) or **table-field-ref-style** (`table`/`id`/`name` attributes, no text content, optional `repetition="N"` attr). Empty slot is the degenerate table-field-ref with all three attrs empty/zero.

**Fields to carry:**

- `Calculation Title`
- `Calculation Message`
- `IReadOnlyList<ShowCustomDialogButton> Buttons` — **fixed length 3**, exposing a public shape of `(Calculation? Label, bool CommitState)` per slot. `Label == null` represents an unused slot.
- `IReadOnlyList<ShowCustomDialogInputField>? InputFields` — null when the whole container is absent; otherwise **fixed length 3**, shape `(InputFieldTarget Target, Calculation? Label, bool UsePasswordCharacter)`. `Target` is a discriminated union over variable-ref, table-field-ref, and empty.

**Verbatim XML samples** *(captured from FM Pro via Raw Clipboard Viewer — exact fixtures live in `FileMakerDbs/` scripts)*:

```xml
<!-- Minimal: Title + Message, default OK button, no input fields -->
<Step enable="True" id="87" name="Show Custom Dialog">
  <Title>
    <Calculation><![CDATA["Title Calculation or Text"]]></Calculation>
  </Title>
  <Message>
    <Calculation><![CDATA["Body calculation or text"]]></Calculation>
  </Message>
  <Buttons>
    <Button CommitState="True">
      <Calculation><![CDATA["OK"]]></Calculation>
    </Button>
    <Button CommitState="False"></Button>
    <Button CommitState="False"></Button>
  </Buttons>
</Step>
```

```xml
<!-- Three custom labeled buttons, mixed CommitState: OK(commit) / Cancel(no-commit) / Abort(no-commit) -->
<Step enable="True" id="87" name="Show Custom Dialog">
  <Title>
    <Calculation><![CDATA["Title Calculation or Text"]]></Calculation>
  </Title>
  <Message>
    <Calculation><![CDATA["Body calculation or text"]]></Calculation>
  </Message>
  <Buttons>
    <Button CommitState="True">
      <Calculation><![CDATA["OK"]]></Calculation>
    </Button>
    <Button CommitState="False">
      <Calculation><![CDATA["Cancel"]]></Calculation>
    </Button>
    <Button CommitState="False">
      <Calculation><![CDATA["Abort"]]></Calculation>
    </Button>
  </Buttons>
</Step>
```

```xml
<!-- One input field bound to a variable, other two slots empty. Note variable-style <Field> has text content, no attrs. -->
<Step enable="True" id="87" name="Show Custom Dialog">
  <Title>
    <Calculation><![CDATA["Title Calculation or Text"]]></Calculation>
  </Title>
  <Message>
    <Calculation><![CDATA["Body calculation or text"]]></Calculation>
  </Message>
  <Buttons>
    <Button CommitState="True">
      <Calculation><![CDATA["OK"]]></Calculation>
    </Button>
    <Button CommitState="False"></Button>
    <Button CommitState="False"></Button>
  </Buttons>
  <InputFields>
    <InputField UsePasswordCharacter="False">
      <Field>$inputBoxVariable</Field>
      <Label>
        <Calculation><![CDATA["Input Box Label Calculation"]]></Calculation>
      </Label>
    </InputField>
    <InputField UsePasswordCharacter="False">
      <Field table="" id="0" name=""></Field>
    </InputField>
    <InputField UsePasswordCharacter="False">
      <Field table="" id="0" name=""></Field>
    </InputField>
  </InputFields>
</Step>
```

```xml
<!-- Three input fields, all bound to table fields, with repetition attribute on the first. -->
<Step enable="True" id="87" name="Show Custom Dialog">
  <Title>
    <Calculation><![CDATA["Title Calculation or Text"]]></Calculation>
  </Title>
  <Message>
    <Calculation><![CDATA["Body calculation or text"]]></Calculation>
  </Message>
  <Buttons>
    <Button CommitState="True">
      <Calculation><![CDATA["OK"]]></Calculation>
    </Button>
    <Button CommitState="False"></Button>
    <Button CommitState="False"></Button>
  </Buttons>
  <InputFields>
    <InputField UsePasswordCharacter="False">
      <Field table="ScriptDefinitionHelper" id="3" repetition="44" name="CreatedBy"></Field>
      <Label>
        <Calculation><![CDATA["Input Box Label Calculation"]]></Calculation>
      </Label>
    </InputField>
    <InputField UsePasswordCharacter="False">
      <Field repetition="2">$$secondInputVariable</Field>
      <Label>
        <Calculation><![CDATA["Second Label Calculation"]]></Calculation>
      </Label>
    </InputField>
    <InputField UsePasswordCharacter="False">
      <Field table="ScriptDefinitionHelper" id="3" name="CreatedBy"></Field>
      <Label>
        <Calculation><![CDATA["Third input label calculation"]]></Calculation>
      </Label>
    </InputField>
  </InputFields>
</Step>
```

**Expected display forms** *(confirmed against FM Pro)*:

- `Show Custom Dialog ["Title Calculation or Text"; "Body calculation or text"]` *(any button configuration — buttons never affect the display line)*
- `Show Custom Dialog ["Title Calculation or Text"; "Body calculation or text"; ScriptDefinitionHelper::CreatedBy[44]; $$secondInputVariable[2]; ScriptDefinitionHelper::CreatedBy]` *(Title, Message, then each non-empty input-field target in slot order, `;`-separated)*

**Notes on the captured XML:**

- **Always-3 invariant on `<Buttons>`:** FM Pro emits three `<Button>` slots unconditionally. Empty slot is `<Button CommitState="False"></Button>` — the element is present but the `<Calculation>` child is absent. On write: always emit three buttons; on display: render only the slots that have a `<Calculation>`.
- **Optional 3-slot invariant on `<InputFields>`:** the container is omitted entirely when no inputs are configured (last "three commit buttons" sample). When present, it's always three slots; empty slots are the degenerate table-field-ref (`<Field table="" id="0" name=""/>`) with no `<Label>`.
- **Button label is optional but `CommitState` is not:** the attribute is mandatory on every `<Button>`, even empty ones. Typed POCO: `bool CommitState` on every slot, `Calculation? Label` (null = unused).
- **`<Field>` inside an `<InputField>` is a discriminated union:**
  - *Variable form* — `<Field>$var</Field>` (optionally with `repetition="N"`). Text content holds the variable name including `$` or `$$`.
  - *Table-field-ref form* — `<Field table="T" id="N" name="F" [repetition="M"]/>` (empty element, attrs carry the reference).
  - *Empty slot form* — table-field-ref with `table=""`, `id="0"`, `name=""`.
  - The discriminant is the presence of text content vs. attributes. Parser must check content first; writer emits the form matching the POCO variant.
- **`repetition` is an XML attribute on `<Field>`, not the `$var[rep]` syntax Set Variable uses.** Carry it as a distinct optional `int? Repetition` on the target value type.
- **`UsePasswordCharacter` is per-`InputField`**, not global. Typed POCO carries it per slot.
- **Display-invisible state that still round-trips:** `CommitState` on unused buttons (always `False` but must be emitted), `UsePasswordCharacter` on empty input slots (always `False` but must be emitted), the empty-slot `<Field>` shape, and the full 3-slot padding on both containers. The typed POCO's `ToXml()` must reproduce this exact shape even though the display line ignores it.
- **Display-line format rules (confirmed for input-field variant):**
  1. Tokens are `;`-separated inside `[...]` (note: no space after the `[`).
  2. Title and Message always first two tokens, rendered as the calculation *source text* (e.g. `"Title Calculation or Text"` — including the enclosing quotes from the CDATA).
  3. Input-field tokens come next, in slot order, one per non-empty slot. Empty slots are skipped entirely (no placeholder rendered).
  4. Input-field target renders as `Table::Field` (table-field-ref) or `$var` / `$$var` (variable). `<Label>` calculations are **not** rendered in the display line — labels are XML-only state.
  5. Repetition renders as a `[N]` suffix on the target when `N != 1`. The default `repetition=1` (or absence of the attribute) is suppressed. Applies to both field-refs (`Table::Field[44]`) and variables (`$$var[2]`).
  6. **Buttons never appear in FM Pro's display line** — regardless of `CommitState` combinations or label text, the three `<Button>` slots are always XML-only state in FM Pro's own rendering. Same for input-field labels (`<Label>`) and `UsePasswordCharacter` — all display-invisible in FM Pro but carried in XML.

- **Lossless-round-trip decision (2026-04-14):** SharpFM extends the display-line grammar for this step with a trailing annotation block that carries all XML state invisible to FM Pro's native display. This is a SharpFM-only extension; FM Pro never consumes SharpFM's display text (FM Pro reads only the binary `Mac-XMSS` XML payload), so the extension is free of interop risk. The extended form is the only form SharpFM's display parser ever accepts — parsing is always extended-form-only, never FM-Pro-shape-only. POCO → XML serialization always emits pure FM-Pro XML regardless of display extensions. Rejected alternatives: state-preserving merge (would silently default buttons for new-from-display-text creation, not zero-loss), form-based side panel (would need per-step UI, deferred), disabling display edit (worst ergonomics).

- **Invisible state to encode in the display extension** *(exhaustive list for Show Custom Dialog)*:
  1. Each `<Button>` slot's `CommitState` (bool) — 3 slots always emitted.
  2. Each `<Button>` slot's `<Calculation>` label — optional per slot.
  3. Each populated `<InputField>`'s `<Label><Calculation>` — calculation expression, may be empty.
  4. Each `<InputField>`'s `UsePasswordCharacter` (bool) — per slot.

- **Extension syntax — settled:** form 3 (bulk-state trailing block) with word tokens, matching the style guide. Concrete grammar:

  ```
  Show Custom Dialog [ "Title" ; "Body" ; ScriptDefinitionHelper::CreatedBy[44] ; $$secondInputVariable[2] ; ScriptDefinitionHelper::CreatedBy ; Buttons: [ "OK" commit ; "Cancel" nocommit ; "" nocommit ] ; Inputs: [ "Input Box Label Calculation" plain ; "Second Label Calculation" plain ; "Third input label calculation" plain ] ]
  ```

  - Button slot: `<Calculation> Label` (quoted calc source, or `""` when empty) followed by `commit`/`nocommit` for `CommitState`. All three slots always rendered.
  - Input-field slot (only when `<InputFields>` is present): `<Label><Calculation>` source followed by `plain`/`password` for `UsePasswordCharacter`. Three slots always rendered when the block is present; empty slot's Label renders as `""`.
  - When `<InputFields>` is absent from the XML (default minimal dialog), the entire `; Inputs: [...]` section is omitted from the display. `; Buttons: [...]` is always emitted because `<Buttons>` is always present in the XML.

- **`<Label>` is always present on a populated `<InputField>` slot**, but its inner `<Calculation>` content may be empty. Typed POCO carries `Calculation Label` (not nullable) on each populated slot; writer always emits the element.
- **Title / Message are arbitrary calculations.** The literal-string fixtures (`"Title Calculation or Text"`) are just the captured examples; any calc expression is legal (e.g. `Get(ScriptName) & " - confirm"`). The display line renders the calc source text verbatim, quotes and all — no unwrapping, no evaluation.

---

### # (comment) *(#89)*

- [x] **Typed POCO landed** — `CommentStep.cs`, tests in `CommentStepTests.cs`

**Why it's special:** Comments render as `# text` without brackets. **A comment is always ONE `<Step>` element**, even when `Text` contains embedded newlines (confirmed against FM Pro fixtures — FM does not split multi-line comments across multiple step elements).

**Fields to carry:** `string Text` (may contain `\n`).

**Verbatim XML samples** *(captured from FM Pro via Raw Clipboard Viewer)*:

```xml
<!-- Single line -->
<Step enable="True" id="89" name="# (comment)">
  <Text>this is a single line comment.</Text>
</Step>
```

```xml
<!-- Multi-line: still ONE step, Text has literal newlines -->
<Step enable="True" id="89" name="# (comment)">
  <Text>this is
many
lines
with
multiple words
on 
some of the 
lines</Text>
</Step>
```

**Display strategy — always render on one line**, substituting embedded newlines with `⏎` (U+23CE RETURN SYMBOL). A multi-line comment's display is a single (possibly long) line with visible return glyphs where newlines live. This matches FileMaker's own behavior — FM displays comments as a single script-editor line regardless of internal line breaks.

**Empty comments render as blank lines.** A `<Step id="89">` whose `<Text>` is empty (or the element is absent) displays as an empty string — a blank line in the editor. Matches FM Pro's script-editor convention where blank-looking lines ARE empty comment steps. On parse, any blank display line round-trips back to an empty `CommentStep`.

**Expected display forms:**
- `# this is a single line comment.`
- `# this is⏎many⏎lines⏎with⏎multiple words⏎on ⏎some of the ⏎lines`
- `` (empty string) — rendering for an empty-Text comment step.

**Line-ending normalization.** FM Pro's clipboard XML commonly uses `&#13;` (CR, `\r`) as the comment-text newline. `CommentStep.FromXml` normalizes any CR/LF/CRLF in `<Text>` to `\n` before storing so CR can never leak into the display document (where AvaloniaEdit would treat it as a visual line break, desyncing the editor's line numbering from `MultiLineStatementRanges`). Round-trip is lossless for `\n`-containing comments; CR-containing comments are normalized to LF on ingest.

**Design notes:**
- `CommentStep.ToDisplayLine` renders `# {Text.Replace("\n", "⏎")}`. No continuation logic whatsoever — comments are always one line.
- `FromDisplayParams` reverses: `⏎ → \n`.
- `FromXml` reads `<Text>` verbatim (preserving any embedded newlines).
- `ToXml` writes `<Text>` verbatim.
- **`ScriptTextParser.MergeCommentContinuations` is deleted.** It was a workaround for the old render path that split multi-line comments across physical editor lines. Under the `⏎` approach there is no split to merge.
- Users who want to author a multi-line comment in the editor can: (a) paste a `⏎` character, or (b) use a future `Shift+Enter` binding in the script editor (not in this change).

**Zero-loss proof:** `<Text>` round-trips byte-for-byte. `⏎` is never a legitimate character in a FileMaker calculation or identifier, so collision with authored content is effectively impossible.

---

### Control flow: If / Else If / Else / End If / Loop / End Loop / Exit Loop If *(#68 / #125 / #69 / #70 / #71 / #73 / #72)*

- [x] **All seven typed POCOs landed** — `ControlFlowSteps.cs`, tests in `ControlFlowStepsTests.cs`

**Why they're special:** `If`, `Else If`, and `Exit Loop If` carry a `Calculation` condition that the display shows as `If [ $x > 0 ]`. The catalog param list for these also includes a `Restore` boolean which should never appear in display — the old handler explicitly read only Calculation and ignored Restore. `Else`, `End If`, `Loop`, `End Loop` have no display params. All seven contribute to the `BlockPair` indentation logic in `FmScript.ToDisplayLines`, which reads `step.Definition?.BlockPair?.Role`.

**Fields to carry:** `Calculation Condition` on the three that need it; empty POCOs for the others.

**Verbatim XML samples** *(captured from FM Pro via Raw Clipboard Viewer for If / Else If / Else / End If; Loop / End Loop / Exit Loop If still pending fresh capture but are trivial — Loop and End Loop have no child elements, Exit Loop If mirrors If's shape)*:

```xml
<!-- If: If [ $variable > $anotherVariable ] -->
<Step enable="True" id="68" name="If">
  <Calculation><![CDATA[$variable > $anotherVariable]]></Calculation>
</Step>
```

```xml
<!-- Else If: multi-line calculation preserves newlines inside CDATA -->
<Step enable="True" id="125" name="Else If">
  <Calculation><![CDATA[Case ( $a > 1; 1;
$b > 3; 4 )]]></Calculation>
</Step>
```

```xml
<!-- Else: no children -->
<Step enable="True" id="69" name="Else"></Step>
```

```xml
<!-- End If: no children -->
<Step enable="True" id="70" name="End If"></Step>
```

```xml
<!-- Loop: no children -->
<Step enable="True" id="71" name="Loop"></Step>
```

```xml
<!-- End Loop: no children -->
<Step enable="True" id="73" name="End Loop"></Step>
```

```xml
<!-- Exit Loop If: same shape as If -->
<Step enable="True" id="72" name="Exit Loop If">
  <Calculation><![CDATA[$variable = $condition]]></Calculation>
</Step>
```

**Expected display forms:**

- `If [ $variable > $anotherVariable ]`
- `Else If [ Case ( $a > 1; 1;\n$b > 3; 4 ) ]` *(newlines inside the CDATA survive round-trip but the display line is single-line; the typed POCO's `Calculation.Text` stores the full multi-line source)*
- `Else`, `End If`, `Loop`, `End Loop` (bare step names, no brackets, no params)
- `Exit Loop If [ $done ]`

**Notes on the captured XML:**

- `<Calculation>` is the only child for If / Else If / Exit Loop If. No `<Restore>` element is present in the FM Pro output — despite the catalog including it as a param, FM Pro omits it, confirming the old handler's "read only Calculation, ignore Restore" behavior is the right one.
- `Else` and `End If` / `End Loop` / `Loop` have no children at all. The typed POCO has no fields beyond `Enabled`.
- Multi-line calcs inside `<Calculation>` CDATA preserve literal newlines byte-for-byte (like Set Field's multi-line calc case). Display rendering collapses to one line by convention but the POCO's `Calculation.Text` carries the raw source.
- **Zero-loss audit (form lookup):** `If` / `Else If` / `Exit Loop If` carry a single `Calculation` — no invisible state, no extension needed. `Else` / `End If` / `Loop` / `End Loop` carry no state — no invisible state, no extension needed. Step category passes the audit by construction.
- Structure note: this is a cluster of 7 steps that share the same empty-or-calc pattern. One file `ControlFlowSteps.cs` holds all seven classes and a single shared `[ModuleInitializer]` method registers them together. Block-pair indentation (Open/Middle/Close) is driven by the `StepDefinition.BlockPair` metadata from the catalog, not by the typed POCOs themselves.

---

## Priority 2 — migrated (reference examples)

### Go to Layout *(#6)*

- [x] **Typed POCO landed** — `GoToLayoutStep.cs`, 9 tests in `GoToLayoutStepTests.cs`

The canonical pattern. Read this code when writing any other typed POCO. Key techniques demonstrated:

- Discriminated union (`LayoutTarget`) for shape-dependent params
- `(#id)` display suffix for lossless id round-trip without schema access
- Accepting both legacy and current wire values in `FromXml` for tolerance
- Always emitting the FM-correct short wire value in `ToXml`
- Regex-based `FromDisplayParams` for structured display tokens
- `[ModuleInitializer]` factory registration with `CA2255` suppression

---

## Priority 3 — long tail *(192 steps on RawStep)*

These steps currently ride the `RawStep` + `CatalogDisplayRenderer` path. They are lossless by construction for source XML (thanks to `RawStep` preserving the element) but may not render with perfect FM Pro fidelity through display-text editing. Migrate on demand: when a user edit workflow exposes a shortcoming, promote that step to a typed POCO.

Steps tagged with `*[type]*` markers have one or more rich-shape params (`complex`, `field`, `script`, `layout`, `tableOccurrence`, etc.) — those are the likely candidates for future migration because their round-trip through display text is more fragile than simple enum/boolean-only steps.

### accounts

- [ ] Change Password *(#83)*
- [ ] Add Account *(#134)*
- [ ] Delete Account *(#135)*
- [ ] Reset Account Password *(#136)*
- [ ] Enable Account *(#137)*
- [ ] Re-Login *(#138)*

### artificial intelligence

- [ ] Configure Machine Learning Model *(#202)* *[field]*
- [ ] Configure AI Account *(#212)*
- [ ] Fine-Tune Model *(#213)* *[complex, field]*
- [ ] Perform SQL Query by Natural Language *(#214)* *[complex, field, tableList]*
- [ ] Insert Embedding *(#215)* *[fieldOrVariable]*
- [ ] Insert Embedding in Found Set *(#216)* *[field]*
- [ ] Set AI Call Logging *(#217)*
- [ ] Perform Semantic Find *(#218)* *[complex, field]*
- [ ] Perform RAG Action *(#219)* *[field]*
- [ ] Generate Response from Model *(#220)* *[complex, field]*
- [ ] Perform Find by Natural Language *(#221)* *[field]*
- [ ] Configure Regression Model *(#222)* *[field]*
- [ ] Configure Prompt Template *(#226)*
- [ ] Configure RAG Account *(#227)*

### control

- [ ] Pause/Resume Script *(#62)*
- [ ] Allow User Abort *(#85)*
- [ ] Set Error Capture *(#86)*
- [ ] Halt Script *(#90)*
- [ ] Exit Script *(#103)*
- [ ] Install OnTimer Script *(#148)* *[script]*
- [ ] Perform Script on Server *(#164)* *[script]*
- [ ] Set Layout Object Animation *(#168)*
- [ ] Configure Region Monitor Script *(#185)* *[script]*
- [ ] Configure Local Notification *(#187)* *[script]*
- [ ] Set Error Logging *(#200)*
- [ ] Configure NFC Reading *(#201)* *[script]*
- [ ] Open Transaction *(#205)*
- [ ] Commit Transaction *(#206)*
- [ ] Revert Transaction *(#207)*
- [ ] Perform Script on Server with Callback *(#210)* *[complex, script]*
- [ ] Set Revert Transaction on Error *(#223)*
- [ ] Trigger Claris Connect Flow

### editing

- [ ] Undo/Redo *(#45)*
- [ ] Cut *(#46)* *[field]*
- [ ] Copy *(#47)* *[field]*
- [ ] Paste *(#48)* *[field]*
- [ ] Clear *(#49)* *[field]*
- [ ] Select All *(#50)*
- [ ] Perform Find/Replace *(#128)* *[complex]*
- [ ] Set Selection *(#130)* *[field]*

### fields

- [ ] Insert from Index *(#11)* *[field]*
- [ ] Insert from Last Visited *(#12)* *[field]*
- [ ] Insert Current Date *(#13)* *[fieldOrVariable]*
- [ ] Insert Current Time *(#14)* *[fieldOrVariable]*
- [ ] Relookup Field Contents *(#40)* *[field]*
- [ ] Insert Picture *(#56)*
- [ ] Insert Current User Name *(#60)* *[fieldOrVariable]*
- [ ] Insert Text *(#61)* *[fieldOrVariable]*
- [ ] Insert Calculated Result *(#77)* *[fieldOrVariable]*
- [ ] Replace Field Contents *(#91)* *[field]*
- [ ] Set Next Serial Value *(#116)* *[field]*
- [ ] Insert File *(#131)* *[fieldOrVariable]*
- [ ] Export Field Contents *(#132)* *[field]*
- [ ] Set Field By Name *(#147)*
- [ ] Insert PDF *(#158)*
- [ ] Insert Audio/Video *(#159)*
- [ ] Insert from URL *(#160)* *[fieldOrVariable]*
- [ ] Insert from Device *(#161)* *[bitmask, field]*

### files

- [ ] Save a Copy as XML *(#3)*
- [ ] Open File *(#33)* *[fileReference]*
- [ ] Close File *(#34)* *[fileReference]*
- [ ] Save a Copy as *(#37)*
- [ ] Print Setup *(#42)* *[complex]*
- [ ] Print *(#43)* *[complex]*
- [ ] New File *(#82)*
- [ ] Set Multi-User *(#84)*
- [ ] Set Use System Formats *(#94)*
- [ ] Recover File *(#95)*
- [ ] Convert File *(#139)*
- [ ] Get File Exists *(#188)* *[field]*
- [ ] Get File Size *(#189)* *[field]*
- [ ] Create Data File *(#190)*
- [ ] Open Data File *(#191)* *[field]*
- [ ] Write to Data File *(#192)* *[field]*
- [ ] Read from Data File *(#193)* *[field]*
- [ ] Get Data File Position *(#194)* *[field]*
- [ ] Set Data File Position *(#195)*
- [ ] Close Data File *(#196)*
- [ ] Delete File *(#197)*
- [ ] Rename File *(#199)*

### found sets

- [ ] Unsort Records *(#21)*
- [ ] Show All Records *(#23)*
- [ ] Modify Last Find *(#24)*
- [ ] Omit Record *(#25)*
- [ ] Omit Multiple Records *(#26)*
- [ ] Show Omitted Only *(#27)*
- [ ] Perform Find *(#28)* *[findRequests]*
- [ ] Sort Records *(#39)* *[complex]*
- [ ] Constrain Found Set *(#126)* *[findRequests]*
- [ ] Extend Found Set *(#127)* *[findRequests]*
- [ ] Perform Quick Find *(#150)*
- [ ] Sort Records by Field *(#154)* *[field]*
- [ ] Find Matching Records *(#155)* *[field]*

### miscellaneous

- [ ] Exit Application *(#44)*
- [ ] Send Event *(#57)* *[complex]*
- [ ] Send Mail *(#63)* *[complex]*
- [ ] Send DDE Execute *(#64)*
- [ ] Dial Phone *(#65)*
- [ ] Speak *(#66)* *[complex]*
- [ ] Perform AppleScript *(#67)*
- [ ] Beep *(#93)*
- [ ] Save a Copy as Add-on Package *(#96)*
- [ ] Flush Cache to Disk *(#102)*
- [ ] Open URL *(#111)*
- [ ] Allow Formatting Bar *(#115)*
- [ ] Execute SQL *(#117)* *[complex]*
- [ ] Install Menu Set *(#142)* *[menuSet]*
- [ ] Set Web Viewer *(#146)*
- [ ] Install Plug-In File *(#157)* *[field]*
- [ ] Refresh Object *(#167)*
- [ ] Enable Touch Keyboard *(#174)*
- [ ] Perform JavaScript in Web Viewer *(#175)* *[complex]*
- [ ] AVPlayer Play *(#177)*
- [ ] AVPlayer Set Playback State *(#178)*
- [ ] AVPlayer Set Options *(#179)*
- [ ] Refresh Portal *(#180)*
- [ ] Get Folder Path *(#181)*
- [ ] Execute FileMaker Data API *(#203)* *[field]*
- [ ] Set Session Identifier *(#208)*

### navigation

- [ ] Go to Previous Field *(#4)*
- [ ] Go to Next Field *(#5)*
- [ ] Go to Field *(#17)* *[field]*
- [ ] Enter Find Mode *(#22)* *[findRequests]*
- [ ] Enter Preview Mode *(#41)*
- [ ] Enter Browse Mode *(#55)*
- [ ] Go to Related Record *(#74)* *[complex, layoutRef, tableOccurrence]*
- [ ] Go to Portal Row *(#99)*
- [ ] Go to Object *(#145)*
- [ ] Close Popover *(#169)*
- [ ] Go to List of Records *(#228)* *[complex]*

### open menu item

- [ ] Open Help *(#32)*
- [ ] Open Manage Database *(#38)*
- [ ] Open Script Workspace *(#88)*
- [ ] Open Settings *(#105)*
- [ ] Open Manage Value Lists *(#112)*
- [ ] Open Sharing *(#113)*
- [ ] Open File Options *(#114)*
- [ ] Open Hosts *(#118)*
- [ ] Open Find/Replace *(#129)*
- [ ] Open Manage Data Sources *(#140)*
- [ ] Open Edit Saved Finds *(#149)*
- [ ] Open Manage Layouts *(#151)*
- [ ] Open Manage Containers *(#156)*
- [ ] Open Manage Themes *(#165)*
- [ ] Open Upload to Host *(#172)*
- [ ] Open Favorites *(#183)*

### records

- [ ] New Record/Request *(#7)*
- [ ] Duplicate Record/Request *(#8)*
- [ ] Delete Record/Request *(#9)*
- [ ] Delete All Records *(#10)*
- [ ] Import Records *(#35)* *[complex, tableRef]*
- [ ] Export Records *(#36)* *[complex]*
- [ ] Revert Record/Request *(#51)*
- [ ] Commit Records/Requests *(#75)*
- [ ] Copy All Records/Requests *(#98)*
- [ ] Copy Record/Request *(#101)*
- [ ] Delete Portal Row *(#104)*
- [ ] Open Record/Request *(#133)*
- [ ] Save Records as Excel *(#143)* *[complex]*
- [ ] Save Records as PDF *(#144)* *[complex]*
- [ ] Save Records as Snapshot Link *(#152)*
- [ ] Truncate Table *(#182)* *[tableReference]*
- [ ] Save Records as JSONL *(#225)* *[field, tableReference]*

### spelling

- [ ] Check Selection *(#18)* *[field]*
- [ ] Check Record *(#19)*
- [ ] Check Found Set *(#20)*
- [ ] Correct Word *(#106)*
- [ ] Spelling Options *(#107)*
- [ ] Select Dictionaries *(#108)*
- [ ] Edit User Dictionary *(#109)*
- [ ] Set Dictionary *(#209)*

### windows

- [ ] Show/Hide Toolbars *(#29)*
- [ ] View As *(#30)*
- [ ] Adjust Window *(#31)*
- [ ] Freeze Window *(#79)*
- [ ] Refresh Window *(#80)*
- [ ] Scroll Window *(#81)*
- [ ] Show/Hide Text Ruler *(#92)*
- [ ] Set Zoom Level *(#97)*
- [ ] Move/Resize Window *(#119)*
- [ ] Arrange All Windows *(#120)*
- [ ] Close Window *(#121)*
- [ ] New Window *(#122)*
- [ ] Select Window *(#123)*
- [ ] Set Window Title *(#124)*
- [ ] Show/Hide Menubar *(#166)*
