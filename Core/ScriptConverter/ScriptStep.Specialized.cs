using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter;

public partial class ScriptStep
{
    // --- Specialized display rendering ---

    private string? ToDisplayLine_Specialized()
    {
        if (Definition == null) return null;

        return Definition.Name switch
        {
            "# (comment)" => ToDisplay_Comment(),
            "Set Variable" => ToDisplay_SetVariable(),
            "Set Field" => ToDisplay_SetField(),
            "Perform Script" => ToDisplay_PerformScript(),
            "Go to Layout" => ToDisplay_GoToLayout(),
            "Go to Record/Request/Page" => ToDisplay_GoToRecord(),
            "Show Custom Dialog" => ToDisplay_ShowCustomDialog(),
            "If" or "Else If" or "Exit Loop If" => ToDisplay_ConditionStep(),
            "Else" or "End If" or "Loop" or "End Loop" => Definition.Name,
            _ => null  // fall through to generic
        };
    }

    // --- Specialized: build XML directly from display params (for FromDisplayLine) ---

    internal static XElement? BuildXmlFromDisplay_Specialized(
        StepDefinition definition, bool enabled, string[] hrParams)
    {
        return definition.Name switch
        {
            "Set Variable" => BuildXml_SetVariable(enabled, hrParams),
            "Set Field" => BuildXml_SetField(enabled, hrParams),
            "Perform Script" => BuildXml_PerformScript(enabled, hrParams),
            "Go to Layout" => BuildXml_GoToLayout(enabled, hrParams),
            "Go to Record/Request/Page" => BuildXml_GoToRecord(enabled, hrParams),
            "Show Custom Dialog" => BuildXml_ShowCustomDialog(enabled, hrParams),
            "If" => BuildXml_Condition(68, "If", enabled, hrParams),
            "Else If" => BuildXml_Condition(125, "Else If", enabled, hrParams),
            "Exit Loop If" => BuildXml_Condition(72, "Exit Loop If", enabled, hrParams),
            "Else" => MakeStepStatic(69, "Else", enabled),
            "End If" => MakeStepStatic(70, "End If", enabled),
            "Loop" => MakeStepStatic(71, "Loop", enabled),
            "End Loop" => MakeStepStatic(73, "End Loop", enabled),
            _ => null
        };
    }

    private static XElement MakeStepStatic(int id, string name, bool enabled)
    {
        return new XElement("Step",
            new XAttribute("enable", enabled ? "True" : "False"),
            new XAttribute("id", id),
            new XAttribute("name", name));
    }

    private static XElement BuildXml_SetVariable(bool enabled, string[] hrParams)
    {
        string varName = "", calcValue = "", repetition = "1";
        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Value:", StringComparison.OrdinalIgnoreCase))
                calcValue = trimmed.Substring(6).TrimStart();
            else if (trimmed.StartsWith("$"))
            {
                var parsed = ParseVarRepetition(trimmed);
                varName = parsed.Name;
                repetition = parsed.Repetition;
            }
        }
        var step = MakeStepStatic(141, "Set Variable", enabled);
        step.Add(XElement.Parse($"<Value><Calculation><![CDATA[{calcValue}]]></Calculation></Value>"));
        step.Add(XElement.Parse($"<Repetition><Calculation><![CDATA[{repetition}]]></Calculation></Repetition>"));
        step.Add(new XElement("Name", varName));
        return step;
    }

    private static XElement BuildXml_SetField(bool enabled, string[] hrParams)
    {
        string fieldTable = "", fieldName = "", calcValue = "";
        if (hrParams.Length >= 1)
        {
            var first = hrParams[0].Trim();
            if (first.Contains("::")) { var parts = first.Split("::", 2); fieldTable = parts[0]; fieldName = parts[1]; }
            else fieldName = first;
        }
        if (hrParams.Length >= 2) calcValue = hrParams[1].Trim();
        var step = MakeStepStatic(76, "Set Field", enabled);
        step.Add(XElement.Parse($"<Calculation><![CDATA[{calcValue}]]></Calculation>"));
        step.Add(new XElement("Field", new XAttribute("table", fieldTable), new XAttribute("id", "0"), new XAttribute("name", fieldName)));
        return step;
    }

    private static XElement BuildXml_PerformScript(bool enabled, string[] hrParams)
    {
        string scriptName = "", param = "";
        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Parameter:", StringComparison.OrdinalIgnoreCase))
                param = trimmed.Substring(10).TrimStart();
            else scriptName = XmlHelpers.Unquote(trimmed);
        }
        var step = MakeStepStatic(1, "Perform Script", enabled);
        step.Add(XElement.Parse($"<Calculation><![CDATA[{param}]]></Calculation>"));
        step.Add(new XElement("Script", new XAttribute("id", "0"), new XAttribute("name", scriptName)));
        return step;
    }

    private static XElement BuildXml_GoToLayout(bool enabled, string[] hrParams)
    {
        string dest = "OriginalLayout", layoutName = "", animation = "";
        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Animation:", StringComparison.OrdinalIgnoreCase))
                animation = trimmed.Substring(10).TrimStart();
            else if (trimmed.StartsWith("Layout Number:", StringComparison.OrdinalIgnoreCase))
                dest = "LayoutNumberByCalculation";
            else if (trimmed == "original layout")
                dest = "OriginalLayout";
            else { dest = "SelectedLayout"; layoutName = XmlHelpers.Unquote(trimmed); }
        }
        var step = MakeStepStatic(6, "Go to Layout", enabled);
        step.Add(new XElement("LayoutDestination", new XAttribute("value", dest)));
        if (dest == "SelectedLayout")
            step.Add(new XElement("Layout", new XAttribute("id", "0"), new XAttribute("name", layoutName)));
        if (!string.IsNullOrEmpty(animation))
            step.Add(new XElement("Animation", new XAttribute("value", animation)));
        return step;
    }

    private static XElement BuildXml_GoToRecord(bool enabled, string[] hrParams)
    {
        string location = "Next", exitState = "False", calc = "";
        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Exit after last:", StringComparison.OrdinalIgnoreCase))
                exitState = trimmed.Substring(16).TrimStart().Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : "False";
            else if (trimmed.StartsWith("By Calculation:", StringComparison.OrdinalIgnoreCase))
                { location = "By Calculation"; calc = trimmed.Substring(15).TrimStart(); }
            else if (trimmed is "First" or "Last" or "Previous" or "Next")
                location = trimmed;
        }
        var step = MakeStepStatic(16, "Go to Record/Request/Page", enabled);
        step.Add(new XElement("RowPageLocation", new XAttribute("value", location)));
        step.Add(new XElement("Exit", new XAttribute("state", exitState)));
        if (!string.IsNullOrEmpty(calc))
            step.Add(XElement.Parse($"<Calculation><![CDATA[{calc}]]></Calculation>"));
        return step;
    }

    private static XElement BuildXml_ShowCustomDialog(bool enabled, string[] hrParams)
    {
        string title = "", message = "";
        var buttons = new List<string>();
        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                title = trimmed.Substring(6).TrimStart();
            else if (trimmed.StartsWith("Message:", StringComparison.OrdinalIgnoreCase))
                message = trimmed.Substring(8).TrimStart();
            else if (trimmed.StartsWith("Buttons:", StringComparison.OrdinalIgnoreCase))
                buttons.AddRange(trimmed.Substring(8).TrimStart().Split(',').Select(b => b.Trim()));
        }
        var step = MakeStepStatic(87, "Show Custom Dialog", enabled);
        step.Add(XElement.Parse($"<Title><Calculation><![CDATA[{title}]]></Calculation></Title>"));
        step.Add(XElement.Parse($"<Message><Calculation><![CDATA[{message}]]></Calculation></Message>"));
        if (buttons.Count > 0)
        {
            var buttonsEl = new XElement("Buttons");
            foreach (var btn in buttons)
                buttonsEl.Add(XElement.Parse($"<Button CommitState=\"True\"><Calculation><![CDATA[{btn}]]></Calculation></Button>"));
            step.Add(buttonsEl);
        }
        return step;
    }

    private static XElement BuildXml_Condition(int id, string name, bool enabled, string[] hrParams)
    {
        var step = MakeStepStatic(id, name, enabled);
        var calc = hrParams.Length > 0 ? hrParams[0].Trim() : "";
        step.Add(XElement.Parse($"<Calculation><![CDATA[{calc}]]></Calculation>"));
        return step;
    }

    // --- Specialized XML serialization (from model with RawXml) ---

    internal XElement? ToXml_Specialized()
    {
        if (Definition == null) return null;

        return Definition.Name switch
        {
            "# (comment)" => ToXml_Comment(),
            "Set Variable" => ToXml_SetVariable(),
            "Set Field" => ToXml_SetField(),
            "Perform Script" => ToXml_PerformScript(),
            "Go to Layout" => ToXml_GoToLayout(),
            "Go to Record/Request/Page" => ToXml_GoToRecord(),
            "Show Custom Dialog" => ToXml_ShowCustomDialog(),
            "If" => ToXml_Condition(68),
            "Else If" => ToXml_Condition(125),
            "Exit Loop If" => ToXml_Condition(72),
            "Else" => ToXml_SelfClosing(69),
            "End If" => ToXml_SelfClosing(70),
            "Loop" => ToXml_SelfClosing(71),
            "End Loop" => ToXml_SelfClosing(73),
            _ => null  // fall through to generic
        };
    }

    // ========== Comment ==========

    private string ToDisplay_Comment()
    {
        var text = GetParamValue("Text") ?? "";
        if (text.Contains('\n'))
        {
            var lines = text.Split('\n');
            return string.Join("\n", lines.Select(l => $"# {l.TrimEnd('\r')}"));
        }
        return $"# {text}";
    }

    private XElement ToXml_Comment()
    {
        var text = GetParamValue("Text") ?? "";
        var step = MakeStep(89, "# (comment)");
        step.Add(new XElement("Text", text));
        return step;
    }

    // ========== Set Variable ==========

    private string ToDisplay_SetVariable()
    {
        // Read from RawXml when available (loaded from FM), otherwise from ParamValues
        string name, value, repetition;
        if (RawXml != null)
        {
            name = RawXml.Element("Name")?.Value ?? "";
            value = RawXml.Element("Value")?.Element("Calculation")?.Value ?? "";
            repetition = RawXml.Element("Repetition")?.Element("Calculation")?.Value ?? "";
        }
        else
        {
            name = GetParamValue("Name") ?? "";
            value = GetNamedCalcValue("Value") ?? "";
            repetition = GetNamedCalcValue("Repetition") ?? "";
        }

        var displayName = name;
        if (!string.IsNullOrEmpty(repetition) && repetition != "1")
            displayName = $"{name}[{repetition}]";

        if (string.IsNullOrEmpty(value))
            return $"Set Variable [ {displayName} ]";

        return $"Set Variable [ {displayName} ; Value: {value} ]";
    }

    private XElement ToXml_SetVariable()
    {
        var step = MakeStep(141, "Set Variable");

        var raw = ScriptLineParser.ParseRaw(
            (Enabled ? "" : "// ") + ToDisplayLine());

        string varName = "";
        string calcValue = "";
        string repetition = "1";

        foreach (var p in raw.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Value:", StringComparison.OrdinalIgnoreCase))
                calcValue = trimmed.Substring(6).TrimStart();
            else if (trimmed.StartsWith("$"))
            {
                var parsed = ParseVarRepetition(trimmed);
                varName = parsed.Name;
                repetition = parsed.Repetition;
            }
        }

        step.Add(XElement.Parse($"<Value><Calculation><![CDATA[{calcValue}]]></Calculation></Value>"));
        step.Add(XElement.Parse($"<Repetition><Calculation><![CDATA[{repetition}]]></Calculation></Repetition>"));
        step.Add(new XElement("Name", varName));
        return step;
    }

    internal static (string Name, string Repetition) ParseVarRepetition(string text)
    {
        var bracketStart = text.IndexOf('[');
        if (bracketStart > 0 && text.EndsWith(']'))
        {
            var name = text.Substring(0, bracketStart);
            var rep = text.Substring(bracketStart + 1, text.Length - bracketStart - 2);
            return (name, rep);
        }
        return (text, "1");
    }

    // ========== Set Field ==========

    private string ToDisplay_SetField()
    {
        var field = RawXml?.Element("Field");
        string fieldRef = "";
        if (field != null)
        {
            var table = field.Attribute("table")?.Value;
            var name = field.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(table) && !string.IsNullOrEmpty(name))
                fieldRef = $"{table}::{name}";
            else if (!string.IsNullOrEmpty(name))
                fieldRef = name;
            else if (!string.IsNullOrEmpty(field.Value))
                fieldRef = field.Value;
        }

        var calc = RawXml?.Element("Calculation")?.Value;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(fieldRef)) parts.Add(fieldRef);
        if (!string.IsNullOrEmpty(calc)) parts.Add(calc);

        return parts.Count == 0 ? "Set Field" : $"Set Field [ {string.Join(" ; ", parts)} ]";
    }

    private XElement ToXml_SetField()
    {
        var step = MakeStep(76, "Set Field");
        var raw = ScriptLineParser.ParseRaw(
            (Enabled ? "" : "// ") + ToDisplay_SetField());

        string fieldTable = "", fieldName = "", calcValue = "";

        if (raw.Params.Length >= 1)
        {
            var first = raw.Params[0].Trim();
            if (first.Contains("::"))
            {
                var parts = first.Split("::", 2);
                fieldTable = parts[0];
                fieldName = parts[1];
            }
            else
                fieldName = first;
        }
        if (raw.Params.Length >= 2)
            calcValue = raw.Params[1].Trim();

        step.Add(XElement.Parse($"<Calculation><![CDATA[{calcValue}]]></Calculation>"));
        step.Add(new XElement("Field",
            new XAttribute("table", fieldTable),
            new XAttribute("id", "0"),
            new XAttribute("name", fieldName)));
        return step;
    }

    // ========== Perform Script ==========

    private string ToDisplay_PerformScript()
    {
        var scriptName = RawXml?.Element("Script")?.Attribute("name")?.Value;
        var param = RawXml?.Element("Calculation")?.Value;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(scriptName)) parts.Add($"\"{scriptName}\"");
        if (!string.IsNullOrEmpty(param)) parts.Add($"Parameter: {param}");

        return parts.Count == 0 ? "Perform Script" : $"Perform Script [ {string.Join(" ; ", parts)} ]";
    }

    private XElement ToXml_PerformScript()
    {
        var step = MakeStep(1, "Perform Script");
        var raw = ScriptLineParser.ParseRaw(
            (Enabled ? "" : "// ") + ToDisplay_PerformScript());

        string scriptName = "", param = "";
        foreach (var p in raw.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Parameter:", StringComparison.OrdinalIgnoreCase))
                param = trimmed.Substring(10).TrimStart();
            else
                scriptName = XmlHelpers.Unquote(trimmed);
        }

        step.Add(XElement.Parse($"<Calculation><![CDATA[{param}]]></Calculation>"));
        step.Add(new XElement("Script", new XAttribute("id", "0"), new XAttribute("name", scriptName)));
        return step;
    }

    // ========== Go to Layout ==========

    private string ToDisplay_GoToLayout()
    {
        var dest = RawXml?.Element("LayoutDestination")?.Attribute("value")?.Value;
        var layoutName = RawXml?.Element("Layout")?.Attribute("name")?.Value;
        var animation = RawXml?.Element("Animation")?.Attribute("value")?.Value;

        string layoutRef = dest switch
        {
            "OriginalLayout" => "original layout",
            "LayoutNameByCalculation" => RawXml?.Element("Calculation")?.Value ?? "original layout",
            "LayoutNumberByCalculation" => $"Layout Number: {RawXml?.Element("Calculation")?.Value ?? ""}",
            _ => !string.IsNullOrEmpty(layoutName) ? $"\"{layoutName}\"" : "original layout"
        };

        var parts = new List<string> { layoutRef };
        if (!string.IsNullOrEmpty(animation)) parts.Add($"Animation: {animation}");

        return $"Go to Layout [ {string.Join(" ; ", parts)} ]";
    }

    private XElement ToXml_GoToLayout()
    {
        var step = MakeStep(6, "Go to Layout");
        var raw = ScriptLineParser.ParseRaw(
            (Enabled ? "" : "// ") + ToDisplay_GoToLayout());

        string dest = "OriginalLayout", layoutName = "", animation = "";

        foreach (var p in raw.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Animation:", StringComparison.OrdinalIgnoreCase))
                animation = trimmed.Substring(10).TrimStart();
            else if (trimmed.StartsWith("Layout Number:", StringComparison.OrdinalIgnoreCase))
                dest = "LayoutNumberByCalculation";
            else if (trimmed == "original layout")
                dest = "OriginalLayout";
            else
            {
                dest = "SelectedLayout";
                layoutName = XmlHelpers.Unquote(trimmed);
            }
        }

        step.Add(new XElement("LayoutDestination", new XAttribute("value", dest)));
        if (dest == "SelectedLayout")
            step.Add(new XElement("Layout", new XAttribute("id", "0"), new XAttribute("name", layoutName)));
        if (!string.IsNullOrEmpty(animation))
            step.Add(new XElement("Animation", new XAttribute("value", animation)));
        return step;
    }

    // ========== Go to Record/Request/Page ==========

    private string ToDisplay_GoToRecord()
    {
        var location = RawXml?.Element("RowPageLocation")?.Attribute("value")?.Value;
        var exitAfterLast = RawXml?.Element("Exit")?.Attribute("state")?.Value;
        var calc = RawXml?.Element("Calculation")?.Value;

        var parts = new List<string>();
        if (location == "By Calculation" && !string.IsNullOrEmpty(calc))
            parts.Add($"By Calculation: {calc}");
        else if (!string.IsNullOrEmpty(location))
            parts.Add(location);
        if (exitAfterLast == "True")
            parts.Add("Exit after last: On");

        return parts.Count == 0
            ? "Go to Record/Request/Page"
            : $"Go to Record/Request/Page [ {string.Join(" ; ", parts)} ]";
    }

    private XElement ToXml_GoToRecord()
    {
        var step = MakeStep(16, "Go to Record/Request/Page");
        var raw = ScriptLineParser.ParseRaw(
            (Enabled ? "" : "// ") + ToDisplay_GoToRecord());

        string location = "Next", exitState = "False", calc = "";

        foreach (var p in raw.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Exit after last:", StringComparison.OrdinalIgnoreCase))
            {
                var val = trimmed.Substring(16).TrimStart();
                exitState = val.Equals("On", StringComparison.OrdinalIgnoreCase) ? "True" : "False";
            }
            else if (trimmed.StartsWith("By Calculation:", StringComparison.OrdinalIgnoreCase))
            {
                location = "By Calculation";
                calc = trimmed.Substring(15).TrimStart();
            }
            else if (trimmed is "First" or "Last" or "Previous" or "Next")
                location = trimmed;
        }

        step.Add(new XElement("RowPageLocation", new XAttribute("value", location)));
        step.Add(new XElement("Exit", new XAttribute("state", exitState)));
        if (!string.IsNullOrEmpty(calc))
            step.Add(XElement.Parse($"<Calculation><![CDATA[{calc}]]></Calculation>"));
        return step;
    }

    // ========== Show Custom Dialog ==========

    private string ToDisplay_ShowCustomDialog()
    {
        var title = RawXml?.Element("Title")?.Element("Calculation")?.Value;
        var message = RawXml?.Element("Message")?.Element("Calculation")?.Value;
        var buttons = RawXml?.Element("Buttons")?.Elements("Button")
            .Select(b => b.Element("Calculation")?.Value)
            .Where(b => !string.IsNullOrEmpty(b))
            .ToList() ?? new List<string?>();

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(title)) parts.Add($"Title: {title}");
        if (!string.IsNullOrEmpty(message)) parts.Add($"Message: {message}");
        if (buttons.Count > 0) parts.Add($"Buttons: {string.Join(", ", buttons)}");

        return parts.Count == 0
            ? "Show Custom Dialog"
            : $"Show Custom Dialog [ {string.Join(" ; ", parts)} ]";
    }

    private XElement ToXml_ShowCustomDialog()
    {
        var step = MakeStep(87, "Show Custom Dialog");
        var raw = ScriptLineParser.ParseRaw(
            (Enabled ? "" : "// ") + ToDisplay_ShowCustomDialog());

        string title = "", message = "";
        var buttons = new List<string>();

        foreach (var p in raw.Params)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                title = trimmed.Substring(6).TrimStart();
            else if (trimmed.StartsWith("Message:", StringComparison.OrdinalIgnoreCase))
                message = trimmed.Substring(8).TrimStart();
            else if (trimmed.StartsWith("Buttons:", StringComparison.OrdinalIgnoreCase))
                buttons.AddRange(trimmed.Substring(8).TrimStart().Split(',').Select(b => b.Trim()));
        }

        step.Add(XElement.Parse($"<Title><Calculation><![CDATA[{title}]]></Calculation></Title>"));
        step.Add(XElement.Parse($"<Message><Calculation><![CDATA[{message}]]></Calculation></Message>"));

        if (buttons.Count > 0)
        {
            var buttonsEl = new XElement("Buttons");
            foreach (var btn in buttons)
                buttonsEl.Add(XElement.Parse($"<Button CommitState=\"True\"><Calculation><![CDATA[{btn}]]></Calculation></Button>"));
            step.Add(buttonsEl);
        }

        return step;
    }

    // ========== Control flow helpers ==========

    private string ToDisplay_ConditionStep()
    {
        var calc = RawXml?.Element("Calculation")?.Value;
        return string.IsNullOrEmpty(calc)
            ? Definition!.Name
            : $"{Definition!.Name} [ {calc} ]";
    }

    private XElement ToXml_Condition(int id)
    {
        var step = MakeStep(id, Definition!.Name);
        var calc = GetCalcFromParams();
        step.Add(XElement.Parse($"<Calculation><![CDATA[{calc}]]></Calculation>"));
        return step;
    }

    private XElement ToXml_SelfClosing(int id)
    {
        return MakeStep(id, Definition!.Name);
    }

    // ========== Shared helpers ==========

    private XElement MakeStep(int id, string name)
    {
        return new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", id),
            new XAttribute("name", name));
    }

    private string? GetParamValue(string xmlElement)
    {
        return ParamValues.FirstOrDefault(p => p.Definition.XmlElement == xmlElement)?.Value;
    }

    private string? GetNamedCalcValue(string wrapperElement)
    {
        return ParamValues.FirstOrDefault(p => p.Definition.WrapperElement == wrapperElement)?.Value;
    }

    private string GetCalcFromParams()
    {
        // For condition steps, the first param is usually the calculation
        return ParamValues.FirstOrDefault(p =>
            p.Definition.Type is "calculation" or "calc")?.Value ?? "";
    }
}
