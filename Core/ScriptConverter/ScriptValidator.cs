using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SharpFM.Core.ScriptConverter;

public static class ScriptValidator
{
    public static List<ScriptDiagnostic> Validate(string displayText)
    {
        if (string.IsNullOrWhiteSpace(displayText))
            return new List<ScriptDiagnostic>();

        var script = FmScript.FromDisplayText(displayText);
        return script.Validate();
    }

    internal static List<string> GetValidValues(StepParam param)
    {
        var valid = new List<string>();

        if (param.Type is "boolean" or "flagBoolean" or "flagElement")
        {
            if (param.HrEnumValues != null)
                valid.AddRange(param.HrEnumValues.Values.Where(v => v != null)!);
            else if (param.HrValues is { Length: > 0 })
                valid.AddRange(param.HrValues);
            else
            {
                valid.Add("On");
                valid.Add("Off");
            }
        }
        else if (param.Type == "enum")
        {
            if (param.HrEnumValues != null)
                valid.AddRange(param.HrEnumValues.Values.Where(v => v != null)!);
            else if (param.EnumValues != null)
            {
                foreach (var ev in param.EnumValues)
                {
                    if (ev.ValueKind == JsonValueKind.String)
                        valid.Add(ev.GetString()!);
                    else if (ev.ValueKind == JsonValueKind.Object && ev.TryGetProperty("hr", out var hr))
                    {
                        var hrStr = hr.GetString();
                        if (!string.IsNullOrEmpty(hrStr))
                            valid.Add(hrStr);
                    }
                }
            }
        }

        return valid;
    }
}
