using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Stateless per-param validation for catalog-driven steps. Produces the
/// same diagnostics the retired <c>StepParamValue.Validate</c> did, but
/// without materializing a per-param wrapper object. Reuses
/// <see cref="CatalogParamExtractor"/> to pull display values from the
/// source element before applying validation rules.
///
/// <para>
/// Only per-param checks live here — block-pair validation and positional
/// concerns stay in <see cref="ScriptValidator"/>, which operates on text
/// lines rather than element state.
/// </para>
/// </summary>
internal static class CatalogValidator
{
    public static List<ScriptDiagnostic> Validate(XElement stepEl, StepDefinition def, int lineIndex)
    {
        var diagnostics = new List<ScriptDiagnostic>();

        foreach (var paramDef in def.Params)
        {
            var value = CatalogParamExtractor.Extract(stepEl, paramDef);
            if (value == null) continue;

            ValidateParam(value, paramDef, lineIndex, diagnostics);
        }

        return diagnostics;
    }

    private static void ValidateParam(
        string value, StepParam paramDef, int lineIndex, List<ScriptDiagnostic> diagnostics)
    {
        // Only validate enum/boolean params that have an explicit label.
        // Unlabeled params may have been positionally matched to a calculation value,
        // and we don't want to flag "$x > 0" as invalid for a boolean Restore param.
        var label = paramDef.HrLabel
            ?? (paramDef.Type == "namedCalc" && paramDef.WrapperElement != null
                ? paramDef.WrapperElement : null);

        var validValues = ScriptValidator.GetValidValues(paramDef);
        if (validValues.Count > 0 && label != null
            && !validValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            diagnostics.Add(new ScriptDiagnostic(
                lineIndex, 0, 0,
                $"Invalid value '{value}' for {label}. Expected: {string.Join(", ", validValues)}",
                DiagnosticSeverity.Warning));
        }

        if (paramDef.Type is "field" or "fieldOrVariable" && !string.IsNullOrEmpty(value))
        {
            if (!value.Contains("::") && !value.StartsWith("$"))
            {
                diagnostics.Add(new ScriptDiagnostic(
                    lineIndex, 0, 0,
                    $"Expected field reference (Table::Field) or variable ($var), got '{value}'",
                    DiagnosticSeverity.Warning));
            }
        }
    }
}
