using System;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting;

namespace SharpFM.Scripting.Handlers;

internal class SetVariableHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Set Variable"];

    public string? ToDisplayLine(ScriptStep step)
    {
        // Catalog params: [Value(namedCalc), Repetition(namedCalc), Name(text)].
        // Canonical display: "Set Variable [ $name[rep] ; Value: <value> ]"
        // with "[rep]" hidden when repetition is empty or "1".
        string? name = null, value = null, repetition = null;
        foreach (var pv in step.ParamValues)
        {
            if (pv.Definition.XmlElement == "Name") name = pv.Value;
            else if (pv.Definition.WrapperElement == "Value") value = pv.Value;
            else if (pv.Definition.WrapperElement == "Repetition") repetition = pv.Value;
        }

        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(value))
            return "Set Variable";

        var nameWithRep = name ?? "";
        if (!string.IsNullOrEmpty(repetition) && repetition != "1")
            nameWithRep = $"{nameWithRep}[{repetition}]";

        if (value == null)
            return $"Set Variable [ {nameWithRep} ]";

        return $"Set Variable [ {nameWithRep} ; Value: {value} ]";
    }

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
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
        var step = MakeStep(141, "Set Variable", enabled);
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
}
