// Extracted verbatim from clockwork-inspector.html.
// Source: andykear/FileMaker-XML-inspector-open-source — Andrew Kear / Clockwork Creative Technology.
// Licensed CC BY 4.0. See ../LICENSE.md.
// extractStepText(step) — converts one <Step> XML element into a
// human-readable line matching the FileMaker Script Workspace display
// format as closely as possible without a full decompiler.
// Covers the ~25 most common step types specifically; everything else
// falls back to the step name from FM_STEP_IDS.

// ── v2.0: FM26 DDR_INFO TEXT INDEX ───────────────────────────
//
// When the FM26 export is made with "Include details for analysis tools"
// ticked (Has_DDR_INFO="True"), each catalog file carries a DDR_INFO
// footer whose ObjectList holds pre-rendered display text — FileMaker's
// own rendering of every script step and calculation — in elements named
// after the owning object's UUID with a leading underscore:
//
//   <_F6D9DD73-... hash="..." datatype="StepText">Set Error Capture [On]</_...>
//
// Structural elements point at these via <DDRREF kind="StepText">_UUID</DDRREF>.
// We index per-document (WeakMap) so the diff engine can hold two files
// with the same step UUIDs but different rendered text without collision.
window.__ddrTextByDoc = window.__ddrTextByDoc || new WeakMap();

function buildDDRTextIndex(doc, root) {
  const map = new Map();
  const counts = { step_text: 0, calc_text: 0, other: 0 };
  for (const ddr of root.querySelectorAll(':scope > DDR_INFO')) {
    // Children are container elements (Script, Calculation, …) holding
    // ObjectLists; entry tag names start with an underscore + UUID.
    for (const entry of ddr.getElementsByTagName('*')) {
      if (entry.tagName.charCodeAt(0) !== 95) continue; // '_'
      const dt = entry.getAttribute('datatype') || '';
      map.set(entry.tagName, entry.textContent || '');
      if (dt === 'StepText') counts.step_text++;
      else if (/Calc/i.test(dt)) counts.calc_text++;
      else counts.other++;
    }
  }
  window.__ddrTextByDoc.set(doc, map);
  return { available: map.size > 0, entries: map.size, ...counts };
}

function extractStepText(step) {
    // v2.0: prefer FileMaker's OWN rendering when the export carries
    // DDR_INFO (FM26 "Include details for analysis tools"). The DDRREF
    // child's text content is the underscore-prefixed UUID key into the
    // per-document index built by buildDDRTextIndex. Falls through to
    // the hand-built renderer when absent — pre-FM26 files, exports made
    // without the option, or entries the index doesn't resolve.
    const ddrMap = window.__ddrTextByDoc && window.__ddrTextByDoc.get(step.ownerDocument);
    if (ddrMap && ddrMap.size) {
        const ref = step.querySelector(':scope > DDRREF[kind="StepText"]');
        if (ref) {
            const key = (ref.textContent || '').trim();
            const txt = ddrMap.get(key);
            if (txt) {
                const en = step.getAttribute('enable') !== 'False';
                return (en ? '' : '// ') + txt;
            }
        }
    }
    const idStr  = step.getAttribute('id') || '';
    const stepId = parseInt(idStr, 10);
    const dict   = FM_STEP_IDS[stepId];
    const name   = dict ? dict.name : (step.getAttribute('name') || ('Step ' + idStr));
    const enabled = step.getAttribute('enable') !== 'False';
    const prefix  = enabled ? '' : '// ';

    // Helper: get first Calculation text content, trimmed and capped
    function calc(sel) {
        const el = step.querySelector(sel || 'Calculation');
        if (!el) return '';
        let t = (el.textContent || '').trim();
        if (t.length > 120) t = t.slice(0, 117) + '\u2026';
        return t;
    }

    // Helper: get attribute from a child element
    function childAttr(sel, attrName) {
        const el = step.querySelector(sel);
        return el ? (el.getAttribute(attrName) || '') : '';
    }

    switch (stepId) {
        // ── Control flow ─────────────────────────────────────
        case 68:  { const c = calc(); return prefix + 'If [ ' + (c || '\u2026') + ' ]'; }
        case 125: { const c = calc(); return prefix + 'Else If [ ' + (c || '\u2026') + ' ]'; }
        case 69:  return prefix + 'Else';
        case 70:  return prefix + 'End If';
        case 71:  return prefix + 'Loop';
        case 72:  { const c = calc(); return prefix + 'Exit Loop If [ ' + (c || '\u2026') + ' ]'; }
        case 73:  return prefix + 'End Loop';
        case 90:  return prefix + 'Halt Script';
        case 91:  { const c = calc('Result > Calculation') || calc(); return prefix + 'Exit Script [ ' + c + ' ]'; }
        case 62:  return prefix + 'Pause/Resume Script';

        // ── Variables ────────────────────────────────────────
        case 141: {
            const varName = (step.querySelector('Name') || {}).textContent || '';
            const val     = calc('Value > Calculation');
            const rep     = calc('Repetition > Calculation');
            let s = prefix + 'Set Variable [ ' + (varName.trim() || '?');
            if (val)                                s += ' ; ' + val;
            if (rep && rep !== '1' && rep !== '')  s += ' ; rep: ' + rep;
            return s + ' ]';
        }

        // ── Fields ───────────────────────────────────────────
        case 76: {
            const fr  = step.querySelector('FieldReference');
            const tor = fr ? fr.querySelector('TableOccurrenceReference') : null;
            const fld = fr ? (fr.getAttribute('name') || '?') : '?';
            const tbl = tor ? (tor.getAttribute('name') || '') : '';
            const c   = calc();
            return prefix + 'Set Field [ ' + (tbl ? tbl + '::' : '') + fld + ' ; ' + (c || '\u2026') + ' ]';
        }
        case 147: {
            const c = calc();
            return prefix + 'Set Field By Name [ ' + (c || '\u2026') + ' ]';
        }

        // ── Scripts ──────────────────────────────────────────
        case 1:
        case 164:
        case 210: {
            const sr     = step.querySelector('ScriptReference');
            const target = sr ? (sr.getAttribute('name') || '?') : '?';
            const param  = calc();
            const label  = stepId === 1   ? 'Perform Script'
                         : stepId === 164 ? 'Perform Script on Server'
                         :                  'Perform Script on Server with Callback';
            return prefix + label + ' [ ' + target + (param ? ' ; ' + param : '') + ' ]';
        }

        // ── Layouts ──────────────────────────────────────────
        case 6: {
            const lr    = step.querySelector('LayoutReference');
            const lname = lr
                ? (lr.getAttribute('name') || (lr.getAttribute('id') === '0' ? '(by calculation)' : '?'))
                : '?';
            return prefix + 'Go to Layout [ ' + lname + ' ]';
        }
        case 74: return prefix + 'Go to Related Records';

        // ── Error / abort ────────────────────────────────────
        case 86: {
            const state = childAttr('Set', 'state');
            return prefix + 'Set Error Capture [ ' + (state === 'True' ? 'On' : 'Off') + ' ]';
        }
        case 85: {
            const state = childAttr('Set', 'state');
            return prefix + 'Allow User Abort [ ' + (state === 'True' ? 'On' : 'Off') + ' ]';
        }

        // ── Records ──────────────────────────────────────────
        case 7:  return prefix + 'New Record/Request';
        case 8:  return prefix + 'Duplicate Record/Request';
        case 9:  return prefix + 'Delete Record/Request';
        case 75: {
            const ni     = step.querySelector('NoInteract');
            const dialog = ni ? (ni.getAttribute('state') === 'True' ? 'No dialog' : 'With dialog') : '';
            return prefix + 'Commit Records/Requests' + (dialog ? ' [ ' + dialog + ' ]' : '');
        }
        case 28:  return prefix + 'Perform Find';
        case 39:  return prefix + 'Sort Records';
        case 182: return prefix + 'Truncate Table';

        // ── Windows ──────────────────────────────────────────
        case 122: return prefix + 'New Window';
        case 121: return prefix + 'Close Window';
        case 123: return prefix + 'Select Window';

        // ── Data / network ───────────────────────────────────
        case 160: {
            const c = calc('URL > Calculation') || calc();
            return prefix + 'Insert from URL [ ' + (c || '\u2026') + ' ]';
        }
        case 111: {
            const c = calc('URL > Calculation') || calc();
            return prefix + 'Open URL [ ' + (c || '\u2026') + ' ]';
        }
        case 87: return prefix + 'Show Custom Dialog';
        case 63: return prefix + 'Send Mail';
        case 35: return prefix + 'Import Records';
        case 36: return prefix + 'Export Records';

        // ── Comments / whitespace ────────────────────────────
        case 89: {
            const c = (step.textContent || '').trim().replace(/\s+/g, ' ');
            return '# ' + (c.slice(0, 100) || '');
        }

        // ── Fallback: step name + any calc hint ───────────────
        default: {
            const anyCalc = calc();
            return prefix + name + (anyCalc ? ' [ ' + anyCalc + ' ]' : '');
        }
    }
}
