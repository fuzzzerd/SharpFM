using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.StepHandlers;

internal class SetVariableHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["Set Variable"];

    public string? ToDisplayLine(ScriptStep step)
    {
        string name, value, repetition;
        if (step.SourceXml != null)
        {
            name = step.SourceXml.Element("Name")?.Value ?? "";
            value = step.SourceXml.Element("Value")?.Element("Calculation")?.Value ?? "";
            repetition = step.SourceXml.Element("Repetition")?.Element("Calculation")?.Value ?? "";
        }
        else
        {
            name = step.ParamValues.FirstOrDefault(p => p.Definition.XmlElement == "Name")?.Value ?? "";
            value = step.ParamValues.FirstOrDefault(p => p.Definition.WrapperElement == "Value")?.Value ?? "";
            repetition = step.ParamValues.FirstOrDefault(p => p.Definition.WrapperElement == "Repetition")?.Value ?? "";
        }

        var displayName = name;
        if (!string.IsNullOrEmpty(repetition) && repetition != "1")
            displayName = $"{name}[{repetition}]";

        return string.IsNullOrEmpty(value)
            ? $"Set Variable [ {displayName} ]"
            : $"Set Variable [ {displayName} ; Value: {value} ]";
    }

    public XElement? ToXml(ScriptStep step)
    {
        // Re-parse from display for consistent output
        return BuildXmlFromDisplay(step.Definition!, step.Enabled,
            ScriptLineParser.ParseRaw(step.ToDisplayLine()).Params);
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
